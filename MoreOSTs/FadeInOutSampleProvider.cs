// Copyright 2020 Mark Heath
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using NAudio.Wave;

namespace MoreOSTs
{
    /// <summary>
    /// Sample Provider to allow fading in and out
    /// Modified version for MoreOSTs - Includes the ability to check current fade state and start fade part-way
    /// </summary>
    public class FadeInOutSampleProvider : ISampleProvider
    {
        public enum FadeState
        {
            Silence,
            FadingIn,
            FullVolume,
            FadingOut,
        }

        private readonly object lockObject = new object();
        private readonly ISampleProvider source;
        private int fadeSamplePosition;
        private int fadeSampleCount;
        public FadeState fadeState { get; private set; }
        public double fadeProgress => fadeSamplePosition / (double)fadeSampleCount;

        /// <summary>
        /// Creates a new FadeInOutSampleProvider
        /// </summary>
        /// <param name="source">The source stream with the audio to be faded in or out</param>
        /// <param name="initiallySilent">If true, we start faded out</param>
        public FadeInOutSampleProvider(ISampleProvider source, bool initiallySilent = false)
        {
            this.source = source;
            fadeState = initiallySilent ? FadeState.Silence : FadeState.FullVolume;
        }

        /// <summary>
        /// Requests that a fade-in begins (will start on the next call to Read)
        /// </summary>
        /// <param name="fadeDurationInMilliseconds">Duration of fade in milliseconds</param>
        public void BeginFadeIn(double fadeDurationInMilliseconds, double progress = 0)
        {
            lock (lockObject)
            {
                if (progress > 1)
                {
                    fadeState = FadeState.FullVolume;
                    return;
                }
                fadeSampleCount = (int)((fadeDurationInMilliseconds * source.WaveFormat.SampleRate) / 1000);
                fadeSamplePosition = (int)(fadeSampleCount*progress);
                fadeState = FadeState.FadingIn;
            }
        }

        /// <summary>
        /// Requests that a fade-out begins (will start on the next call to Read)
        /// </summary>
        /// <param name="fadeDurationInMilliseconds">Duration of fade in milliseconds</param>
        public void BeginFadeOut(double fadeDurationInMilliseconds, double progress = 0)
        {
            lock (lockObject)
            {
                if (progress > 1)
                {
                    fadeState = FadeState.Silence;
                    return;
                }
                fadeSampleCount = (int)((fadeDurationInMilliseconds * source.WaveFormat.SampleRate) / 1000);
                fadeSamplePosition = (int)(fadeSampleCount*progress);
                fadeState = FadeState.FadingOut;
            }
        }

        /// <summary>
        /// Reads samples from this sample provider
        /// </summary>
        /// <param name="buffer">Buffer to read into</param>
        /// <param name="offset">Offset within buffer to write to</param>
        /// <param name="count">Number of samples desired</param>
        /// <returns>Number of samples read</returns>
        public int Read(float[] buffer, int offset, int count)
        {
            int sourceSamplesRead = source.Read(buffer, offset, count);
            lock (lockObject)
            {
                if (fadeState == FadeState.FadingIn)
                {
                    FadeIn(buffer, offset, sourceSamplesRead);
                }
                else if (fadeState == FadeState.FadingOut)
                {
                    FadeOut(buffer, offset, sourceSamplesRead);
                }
                else if (fadeState == FadeState.Silence)
                {
                    ClearBuffer(buffer, offset, count);
                }
            }
            return sourceSamplesRead;
        }

        private static void ClearBuffer(float[] buffer, int offset, int count)
        {
            for (int n = 0; n < count; n++)
            {
                buffer[n + offset] = 0;
            }
        }

        private void FadeOut(float[] buffer, int offset, int sourceSamplesRead)
        {
            int sample = 0;
            while (sample < sourceSamplesRead)
            {
                float multiplier = 1.0f - (fadeSamplePosition / (float)fadeSampleCount);
                for (int ch = 0; ch < source.WaveFormat.Channels; ch++)
                {
                    buffer[offset + sample++] *= multiplier;
                }
                fadeSamplePosition++;
                if (fadeSamplePosition > fadeSampleCount)
                {
                    fadeState = FadeState.Silence;
                    // clear out the end
                    ClearBuffer(buffer, sample + offset, sourceSamplesRead - sample);
                    break;
                }
            }
        }

        private void FadeIn(float[] buffer, int offset, int sourceSamplesRead)
        {
            int sample = 0;
            while (sample < sourceSamplesRead)
            {
                float multiplier = (fadeSamplePosition / (float)fadeSampleCount);
                for (int ch = 0; ch < source.WaveFormat.Channels; ch++)
                {
                    buffer[offset + sample++] *= multiplier;
                }
                fadeSamplePosition++;
                if (fadeSamplePosition > fadeSampleCount)
                {
                    fadeState = FadeState.FullVolume;
                    // no need to multiply any more
                    break;
                }
            }
        }

        /// <summary>
        /// WaveFormat of this SampleProvider
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return source.WaveFormat; }
        }
    }
}