using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BattleNetPrefill.Structs
{
    /// <summary>
    /// Files are grouped on Blizzard's CDNs in one of three folders.
    /// The url to access these files will be build with the following format :
    ///     http://{host}/{productPath}/{rootFolder}
    /// 
    /// See more: https://wowdev.wiki/TACT#File_types
    /// </summary>
    [JsonConverter(typeof(RootFolderJsonConverter))]
    public class RootFolder : EnumBase<RootFolder>
    {
        /// <summary>
        /// Contains archives, indexes, and unarchived standalone files (typically binaries, mp3s, and movies)
        /// </summary>
        public static readonly RootFolder data = new RootFolder("data");

        /// <summary>
        /// Contains the three types of config files: Build configs, CDN configs, and Patch configs
        /// </summary>
        public static readonly RootFolder config = new RootFolder("config");

        /// <summary>
        /// Contains patch manifests, files, archives, indexes
        /// </summary>
        public static readonly RootFolder patch = new RootFolder("patch");

        private RootFolder(string name) : base(name)
        {
        }
    }
    
    public class RootFolderJsonConverter : JsonConverter<RootFolder>
    {
        public override RootFolder Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return RootFolder.Parse(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, RootFolder value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}