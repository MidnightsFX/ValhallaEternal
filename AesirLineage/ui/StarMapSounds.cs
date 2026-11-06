using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using AesirLineage.starmap;

namespace AesirLineage.ui {
    /// <summary>
    /// The sound a star makes when it is taken: one per tier, the same for every deity. Kept as 16-bit
    /// PCM WAVs in the assembly and turned into clips the first time each is needed.
    /// </summary>
    internal static class StarMapSounds {
        private static readonly Dictionary<NodeTier, AudioClip> clips = new Dictionary<NodeTier, AudioClip>();

        /// <summary>A 2D source for the star map, mixed with the game's other interface sounds.</summary>
        internal static AudioSource CreateSource(GameObject host) {
            AudioSource source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            return source;
        }

        internal static void PlayTaken(AudioSource source, NodeTier tier) {
            if (source == null) { return; }
            AudioClip clip = Clip(tier);
            if (clip == null) { return; }
            // AudioMan lives in the game scene, so the mixer is looked up when there is a sound to play.
            if (source.outputAudioMixerGroup == null && AudioMan.instance != null) {
                source.outputAudioMixerGroup = AudioMan.instance.m_guiMixer;
            }
            source.PlayOneShot(clip);
        }

        private static AudioClip Clip(NodeTier tier) {
            if (clips.TryGetValue(tier, out AudioClip clip)) { return clip; }
            string name = tier switch {
                NodeTier.Minor => "star_minor",
                NodeTier.Major => "star_major",
                NodeTier.Oath => "star_oath",
                NodeTier.Apex => "star_apex",
                _ => null,
            };
            clip = name != null ? Load(name) : null;
            // Remembered even when missing, so a broken resource is reported once.
            clips[tier] = clip;
            return clip;
        }

        private static AudioClip Load(string name) {
            string resource = $"AesirLineage.sounds.{name}.wav";
            try {
                using (Stream stream = typeof(StarMapSounds).Assembly.GetManifestResourceStream(resource)) {
                    if (stream == null) {
                        Logger.LogWarning($"Star map sound {resource} is missing.");
                        return null;
                    }
                    using (MemoryStream memory = new MemoryStream()) {
                        stream.CopyTo(memory);
                        return FromWav(name, memory.ToArray());
                    }
                }
            } catch (Exception e) {
                Logger.LogWarning($"Could not load star map sound {resource}: {e.Message}");
                return null;
            }
        }

        // Walks the RIFF chunks for the format and the samples; only 16-bit PCM is expected.
        private static AudioClip FromWav(string name, byte[] wav) {
            int channels = 0;
            int rate = 0;
            int bits = 0;
            int dataStart = -1;
            int dataLength = 0;
            int pos = 12;
            while (pos + 8 <= wav.Length) {
                string id = System.Text.Encoding.ASCII.GetString(wav, pos, 4);
                int size = BitConverter.ToInt32(wav, pos + 4);
                if (id == "fmt ") {
                    channels = BitConverter.ToInt16(wav, pos + 10);
                    rate = BitConverter.ToInt32(wav, pos + 12);
                    bits = BitConverter.ToInt16(wav, pos + 22);
                } else if (id == "data") {
                    dataStart = pos + 8;
                    dataLength = Math.Min(size, wav.Length - dataStart);
                }
                pos += 8 + size + (size & 1);
            }
            if (bits != 16 || channels <= 0 || rate <= 0 || dataStart < 0) {
                Logger.LogWarning($"Star map sound {name} is not 16-bit PCM.");
                return null;
            }
            float[] samples = new float[dataLength / 2];
            for (int i = 0; i < samples.Length; i++) {
                samples[i] = BitConverter.ToInt16(wav, dataStart + i * 2) / 32768f;
            }
            AudioClip clip = AudioClip.Create(name, samples.Length / channels, channels, rate, false);
            SetData.Invoke(clip, new object[] { samples, 0 });
            return clip;
        }

        // Unity 6 adds a ReadOnlySpan overload of SetData, which net48 has no type for, so the compiler
        // can't choose between them; the float[] one is called by name instead.
        private static readonly System.Reflection.MethodInfo SetData =
            typeof(AudioClip).GetMethod(nameof(AudioClip.SetData), new[] { typeof(float[]), typeof(int) });
    }
}
