using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NAudio;
// using NAudio.Extras;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using UnityEngine;

namespace MoreOSTs
{
    public class SongPlayer : IDisposable
    {
        public MoreOSTs Plugin;
        private WaveOutEvent outputDevice;
        private MixingSampleProvider outputMixer;
        private object lockObject = new object();

        public SongManager.Song? CurrentSong { get; protected set; }
        public event Action SongEnding;

        private float volume = 1.0f;
        public float Volume
        {
            get => volume;
            set
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (value != volume)
                {
                    volume = value;
                    if(currentSongAudioFileReader != null && currentSong != null)
                        currentSongAudioFileReader.Volume = value * currentSong.Value.Volume;
                }
            }
        }

        public bool Paused
        {
            get;
            private set;
        } = false;

        public const double FadeDuration = 500; //Fade duration in milliseconds

        public SongPlayer(MoreOSTs plugin)
        {
            Plugin = plugin;
            outputDevice = new WaveOutEvent();
            outputMixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
            outputMixer.ReadFully = true;

            outputMixer.MixerInputEnded += MixerInputEnded;
            
            outputDevice.Init(outputMixer);
            outputDevice.Play();

            outputDevice.PlaybackStopped += PlaybackStopped;
        }

        public void Play(SongManager.Song song)
        {
            lock (lockObject)
            {
                if (songPromise is { IsCompleted: false })
                {
                    songPromise.ContinueWith(songReader => songReader.Result.Dispose());
                    songPromise = null;
                }
                
                replaySong = CurrentSong?.Name == song.Name;
                CurrentSong = song;
                string songPath = $"file:{song.FilePath}";
                songPromise = Task.Run(() => new AudioFileReader(songPath));
                Plugin.logger.LogDebug($"Loading song: {song.Name}");
            }
        }

        public void Stop()
        {
            lock (lockObject)
            {
                CurrentSong = null;
                songPromise = null;
            }
        }

        public void StopImmediate()
        {
            PlaySongInternal(null);
        }

        public void Pause()
        {
            lock(lockObject)
                Paused = true;
        }

        public void Resume()
        {
            lock(lockObject)
                Paused = false;
        }
        
        private AudioFileReader currentSongAudioFileReader;
        private WaveStream currentWaveStream;
        private FadeInOutSampleProvider currentFader;
        private WdlResamplingSampleProvider currentResampler;
        
        private SongManager.Song? currentSong;
        private bool replaySong = false;
        private Task<AudioFileReader> songPromise;

        protected void PlaybackStopped(object sender, StoppedEventArgs stoppedEventArgs)
        {
            Plugin.logger.LogDebug($"Playback stopped: {stoppedEventArgs.Exception}, {outputDevice.PlaybackState}");
            // deviceReady = false;
            // SongEnding?.Invoke();
        }

        private void MixerInputEnded(object sender, SampleProviderEventArgs e)
        {
            Plugin.logger.LogDebug("Mixer input ended");
            if (e.SampleProvider == currentFader)
            {
                Plugin.logger.LogDebug("Clearing state");
                // currentFader = null;
                // PlaySongInternal(null);
                currentFader.BeginFadeOut(0, 2.0);
            }

            if (e.SampleProvider is FadeInOutSampleProvider fader)
            {
                fader.BeginFadeOut(0, 2.0); //Immediately sets fader to silenced state
            }
        }
        
        protected void PlaySongInternal(SongManager.Song? song)
        {
            if(currentFader != null)
                outputMixer?.RemoveMixerInput(currentFader);
            currentWaveStream?.Dispose();

            if (song != null)
            {
                try
                {
                    if(songPromise?.IsCompletedSuccessfully ?? false)
                    {
                        Plugin.logger.LogDebug($"Playing song: {song.Value.Name}, {song.Value.FilePath}");
                        WaveStream audioStream = currentSongAudioFileReader = songPromise.Result;
                        
                        currentSongAudioFileReader.Volume = Volume * song.Value.Volume;

                        if (song.Value.Loop)
                            audioStream = new LoopStream(audioStream);

                        currentWaveStream = audioStream;
                        currentResampler = new WdlResamplingSampleProvider(audioStream.ToSampleProvider(),
                            outputMixer.WaveFormat.SampleRate);

                        currentFader = new FadeInOutSampleProvider(currentResampler);
                        currentFader.BeginFadeIn(FadeDuration);
                        
                        outputMixer.AddMixerInput(currentFader);
                        
                        // deviceReady = true;
                        currentSong = song;
                        
                        //Cleanup
                        songPromise = null;
                        replaySong = false;
                    }
                    else if (songPromise?.IsCompleted ?? false)
                    {
                        Plugin.logger.LogDebug($"Song loading faulted: {songPromise.Exception}");
                        songPromise = null;
                    }
                }
                catch (Exception e)
                {
                    Plugin.logger.LogError($"Song playback failed, aborting.");
                    CurrentSong = null;
                    throw;
                }
            }
            else
            {
                // deviceReady = false;
                currentFader = null;
                currentSongAudioFileReader = null;
                currentSong = null;
            }
        }
        
        private double timeLeftMilliseconds => currentWaveStream == null
                ? -1
                : (currentWaveStream.TotalTime - currentWaveStream.CurrentTime).TotalMilliseconds;
        
        public void Update()
        {
            if (currentSong?.Name != CurrentSong?.Name || replaySong)
            {
                if (currentFader != null)
                {
                    switch (currentFader.fadeState)
                    {
                        case FadeInOutSampleProvider.FadeState.Silence:
                            PlaySongInternal(CurrentSong);
                            break;
                        case FadeInOutSampleProvider.FadeState.FullVolume:
                            if (!replaySong || timeLeftMilliseconds < FadeDuration)
                            {
                                Plugin.logger.LogDebug("Fading out");
                                currentFader.BeginFadeOut(FadeDuration);
                            }

                            break;
                        case FadeInOutSampleProvider.FadeState.FadingIn:
                            Plugin.logger.LogDebug("Fading out partway through");
                            currentFader.BeginFadeOut(FadeDuration, 1.0-currentFader.fadeProgress);
                            break;
                    }
                }
                else
                {
                    PlaySongInternal(CurrentSong);
                }
            }
            else if(CurrentSong.HasValue && currentFader != null)
            {
                switch (currentFader.fadeState)
                {
                    case FadeInOutSampleProvider.FadeState.FadingOut:
                        currentFader.BeginFadeIn(FadeDuration, 1.0-currentFader.fadeProgress);
                        break;
                }
            }
            // else if (songPromise?.IsCompleted ?? false)
            // {
            //     Plugin.logger.LogDebug("TEST");
            //     songPromise = null;
            // }

            if (Paused && outputDevice.PlaybackState == PlaybackState.Playing)
            { 
                Plugin.logger.LogDebug("Pausing");
                outputDevice.Pause();
            }
            
            if (!Paused && outputDevice.PlaybackState == PlaybackState.Paused)
            { 
                Plugin.logger.LogDebug("Unpausing");
                outputDevice.Play();
            }

            // Plugin.logger.LogDebug($"Song left: {timeLeftMilliseconds} {deviceReady}");
            //TODO: Test detecting when song stops playing and playing another
            if (!(currentSong?.Loop ?? false) &&
                (currentFader?.fadeState == FadeInOutSampleProvider.FadeState.FullVolume || currentFader?.fadeState == FadeInOutSampleProvider.FadeState.FadingIn) &&
                currentWaveStream != null && timeLeftMilliseconds < FadeDuration + 100)
            {
                Plugin.logger.LogDebug($"Song running out: {timeLeftMilliseconds}");
                SongEnding?.Invoke();   
            }

            //TODO: Original code runs currentSong.Position = currentSong.Length - 1; and stops device when song runs out, why?
        }

        public void Dispose()
        {
            outputDevice?.Dispose();
            currentSongAudioFileReader?.Dispose();
            currentWaveStream?.Dispose();
            songPromise?.Dispose();
        }
    }
}