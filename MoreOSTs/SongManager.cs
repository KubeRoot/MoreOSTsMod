using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Random = UnityEngine.Random;

namespace MoreOSTs
{
    public class SongManager
    {
        public MoreOSTs Plugin;
        public static string[] AllowedExtensions = { ".wav", ".mp3" };

        public Serialization.settings RawSettings;
        public Song[] Songs;
        public Dictionary<string, string> AudioFilenames;

        public struct Song
        {
            public string Name;
            public string[] Scenes;
            public bool Boss;
            public float Volume;
            public bool Loop;

            public string FilePath;

            public Song(Serialization.song rawSong)
            {
                Name = rawSong.name;
                Scenes = rawSong.scenes.Split(',').Select(scene => scene.Trim()).ToArray();
                Boss = rawSong.boss;
                Volume = rawSong.volume;
                Loop = rawSong.loop;

                FilePath = null;
            }
        }

        public SongManager(MoreOSTs plugin)
        {
            Plugin = plugin;
        }

        public void Load(string settingsPath, string songsPath)
        {
            RawSettings = Serialization.settings.Deserialize(settingsPath);

            AudioFilenames = new Dictionary<string, string>(
                Directory.EnumerateFiles(songsPath, "*", SearchOption.AllDirectories)
                .Where(path => AllowedExtensions.Contains(Path.GetExtension(path)))
                .Select(path => new KeyValuePair<string, string>(Path.GetFileName(path), path)));
            
            Songs = RawSettings.music
                .Select(rawSong => new Song(rawSong)
            {
                FilePath = AudioFilenames.GetValueOrDefault(rawSong.name)
            }).ToArray();

            if (Songs.Any(song => song.FilePath == null))
            {
                foreach (var song in Songs.Where(song => song.FilePath == null))
                {
                    Plugin.logger.LogError($"Music file not found for song \"{song.Name}\"");
                }

                Songs = Songs.Where(song => song.FilePath != null).ToArray();
            }
        }
        
        public Song? PickSong(string scene, bool? boss, params string[] excludedSongs)
        {
            var validSongs = Songs.AsEnumerable();

            //Filter out based on scene
            if (scene != null)
                validSongs = validSongs.Where(song => song.Scenes.Any(scene.Contains));

            //Filter out based on excluded songs
            if (excludedSongs.Length > 0)
                validSongs = validSongs.Where(song => !excludedSongs.Contains(song.Name));

            //Filter out songs based on whether they're boss music or not
            if(boss.HasValue)
                validSongs = validSongs.Where(song => song.Boss == boss.Value);

            var validSongsArray = validSongs.ToArray();
            
            //If no valid songs are found, return null
            if (validSongsArray.Length == 0)
                return null;

            //Choose a random song from the currently valid ones
            int index = Random.Range(0, validSongsArray.Length);

            return validSongsArray[index];
        }
    }
}