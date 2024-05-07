namespace BattleNetPrefill.Utils
{
    public static class DirectorySearch
    {
        /// <summary>
        /// Searches upwards for the first .sln file that is found.  Will likely be the .sln for the current project.
        /// </summary>
        /// <param name="currentPath"></param>
        /// <returns>DirectoryInfo for the .sln containing folder.</returns>
        public static DirectoryInfo TryGetSolutionDirectory(string currentPath = null)
        {
            var directory = new DirectoryInfo(currentPath ?? Directory.GetCurrentDirectory());
            while (directory != null && !directory.GetFiles("*.sln").Any())
            {
                directory = directory.Parent;
            }
            return directory;
        }
    }
}