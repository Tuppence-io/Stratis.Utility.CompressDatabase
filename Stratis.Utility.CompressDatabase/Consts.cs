namespace Stratis.Utility.CompressDatabase
{
    public static class Consts
    {
        // Name of the temp table to use when moving things around.
        public const string TempTableName = "CompressTempTable";

        // Sub-Directories / Database names for the Stratis/DBreeze data files
        public static readonly string[] Repositories = new string[]
        {
                "Blocks",
                "Chain",
                "FinalizedBlock",
                "CoinView",
        };
    }
}
