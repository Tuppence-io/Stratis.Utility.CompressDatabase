# Stratis.Utility.CompressDatabase
A simple utility that will unload/reload data for a [Stratis Full Node](https://github.com/stratisproject/StratisBitcoinFullNode) (DBreeze database).

This was designed to provide a workaround to the following issues.
1. [Size of CoinView UTXO Data](https://github.com/stratisproject/StratisBitcoinFullNode/issues/2414)
    
1. [Removes in DBreeze are logical and the data remains in the file.](https://github.com/hhblaze/DBreeze/issues/21#issuecomment-293054680) 
    This can cause the files on disk to be larger for tables that have a large set of transaction where the data being removed.

1. [Performance of Ascending Keys](https://github.com/hhblaze/DBreeze/blob/master/Documentation/_DBreeze.Documentation.actual.pdf) 
    >DBreeze insert and select algorithms work with maximum efficiency
in bulk operations, when keys are supplied sorted in ascending order (descending is a
bit slower). So, sort bulk chunks in memory before inserts/selects

By unloading and reloading the data into a new table the orphaned records are removed from the file system and records are reinserted in Ascending order.  

### Command Line Examples

This will run the compress in place, just creating and renaming table in the current database.  Please make sure that you have enought disk space.
$> dotnet Stratis.Utility.CompressDatabase.dll -- compress-inplace -datadir=C:\wher\my\data\is

This will run th ecompress using an another disk for temp storage.  Use this option only if you do not have enought disk space for an in place process.
`$> dotnet Stratis.Utility.CompressDatabase.dll -- compress-external -datadir=C:\where\my\data\is\ -tempdir=D:\someplace\else\`


Note:  The data directory is the root directory (same directory where the bitcoin.conf or stratis.conf file is located).

## Results
While this workaround does provide some relief, the results where a bit underwhelming as I was only able to see about a ~4% reduction in the overall size of the files on disk.  In my case that was about 25GB so it provided some benefit but I was hoping more.  As for the performance, I was able to see some small improvements in basic access times and the overall startup time of my FullNode but only by about 2%.

I decide go head and post this utility online just in case someone else may find it useful.
