namespace BuildBackup.Structs
{
    //TODO comment
    public enum RootFolder
    {
        //TODO these being converted to a string might be slowing things down. Precompute them?
        data,
        config,
        patch
    }
}
