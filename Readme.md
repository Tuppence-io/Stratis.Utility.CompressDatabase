# Stratis.Utility.CompressDatabase
A simple utility that will unload/reload data for a Stratis Full Node (DBreeze database).

This was designed to provide a workaround to the following issues.
1. [Size of CoinView UTXO Data](https://github.com/stratisproject/StratisBitcoinFullNode/issues/2414)
    
1. [Removes in DBreeze are logical and the data remains in the file.](https://github.com/hhblaze/DBreeze/issues/21#issuecomment-293054680) 
    This can cause the files on disk to be larger for tables that have a large set of transaction where the data being removed.

1. [Preformance of Ascending Keys](https://github.com/hhblaze/DBreeze/blob/master/Documentation/_DBreeze.Documentation.actual.pdf) 
    >DBreeze insert and select algorithms work with maximum efficiency
in bulk operations, when keys are supplied sorted in ascending order (descending is a
bit slower). So, sort bulk chunks in memory before inserts/selects

By unloading and reloading the data into a new table the orphaned records are removed from the file system and recoreds are reinserted in Ascsending order.  

## Results
While this workaround does provide some relief, the results where a bit underwelming as I was only able to see about a ~4% reduction in the overall size of the files on disk.  In my case that was about 25GB so it provieded some benifit but I was hoping more.  As for the preformance, I was able to see some small improvements in basic access times and the overall startup time of my FullNode but only by about 2%.

I decied go head and post this utility online just in case someone else may find it useful.
