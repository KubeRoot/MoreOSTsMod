using System;
using System.Collections;
using NAudio.Extras;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using UnityEngine;

namespace MoreOSTs
{
    public class SongPlayer
    {
        public MoreOSTs Plugin;
        private WaveOutEvent outputDevice;
        private object lockObject = new object();

        public SongManager.Song? CurrentSong;
        public event Action SongEnding;

        private float volume = 1.0f;
        public float Volume
        {
            get => volume;
            set
            {
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
        }

        public const double FadeDuration = 1500; //Fade duration in milliseconds

        public SongPlayer(MoreOSTs plugin)
        {
            Plugin = plugin;
            outputDevice = new WaveOutEvent();
        }

        public void Play(SongManager.Song song)
        {
            lock(lockObject)
                CurrentSong = song;
        }

        public void Stop()
        {
            lock(lockObject)
                CurrentSong = null;
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
        
        private FadeInOutSampleProvider currentFader;
        private SongManager.Song? currentSong;
        private AudioFileReader currentSongAudioFileReader;
        private bool deviceReady = false;
        
        protected void PlaySongInternal(SongManager.Song? song)
        {
            outputDevice.Stop();

            if (song != null)
            {
                WaveStream audioStream = currentSongAudioFileReader = new AudioFileReader(song.Value.FullFilePath);

                currentSongAudioFileReader.Volume = Volume * song.Value.Volume;

                if (song.Value.Loop)
                    audioStream = new LoopStream(audioStream);
                
                outputDevice.Stop();

                currentFader = new FadeInOutSampleProvider(new WaveToSampleProvider(audioStream));
                currentFader.BeginFadeIn(FadeDuration);

                outputDevice.Init(currentFader);
                outputDevice.Volume = Volume;

                if (!Paused)
                    outputDevice.Play();

                deviceReady = true;
            }
            else
            {
                deviceReady = false;
                currentFader = null;
                currentSongAudioFileReader = null;
            }
            
            currentSong = song;
        }
        
        public void Update()
        {
            if (currentSong?.Name != CurrentSong?.Name)
            {
                if (currentFader != null)
                {
                    switch (currentFader.fadeState)
                    {
                        case FadeInOutSampleProvider.FadeState.Silence:
                            PlaySongInternal(CurrentSong);
                            break;
                        case FadeInOutSampleProvider.FadeState.FullVolume:
                            currentFader.BeginFadeOut(FadeDuration);
                            break;
                        case FadeInOutSampleProvider.FadeState.FadingIn:
                            currentFader.BeginFadeOut(FadeDuration, currentFader.fadeProgress);
                            break;
                    }
                }
                else
                {
                    PlaySongInternal(CurrentSong);
                }
            }

            if (Paused && outputDevice.PlaybackState == PlaybackState.Playing)
                outputDevice.Pause();

            if (!Paused && (outputDevice.PlaybackState == PlaybackState.Paused ||
                            (outputDevice.PlaybackState == PlaybackState.Stopped && deviceReady)))
                outputDevice.Play();

            //TODO: Test detecting when song stops playing and playing another
            if ((currentSong?.Loop ?? false) &&
                currentFader?.fadeState == FadeInOutSampleProvider.FadeState.FullVolume &&
                (currentSongAudioFileReader.TotalTime - currentSongAudioFileReader.CurrentTime).Milliseconds <
                FadeDuration + 100)
                SongEnding?.Invoke();
            
            //TODO: Original code runs currentSong.Position = currentSong.Length - 1; and stops device when song runs out, why?
        }
    }
}