QuikZIV
-------

**QuikZIV** is an uncompressing tool for *ZIV* and *SQZ* files, using either *LZW* or *Huffman+RLE* compression. Developed to remove the protection of the CD-ROM version of *Quik The Thunder Rabbit*, it supports most (if not all) of *Titus Interactive* older games:

- Prehistorik
- Quik The Thunder Rabbit
- Super Cauldron
- Titus The Fox (and Moktar)
- The Blues Brothers (series)

Some games, like *Prehistorik 2*, have some files that are not supported, either because of a different compression method or an additional level of protection.

For more in-depth information and details visit the [DOS Game Modding Wiki](http://www.shikadi.net/moddingwiki/Titus_Interactive_SQZ_Compression). Credits:

- [Jesses](http://ttf.mine.nu/techdocs.htm) for all the research on the compression and the original uncompression implementation in pseudocode and Perl (in which I *strongly* based on).
- [Eirik](http://www.shikadi.net/moddingwiki/User:Eirik) for the UnSQZ unpacker and the great OpenTitus project.
- [Frenkel](http://www.shikadi.net/moddingwiki/User:Frenkel) for the QuickBASIC implementation of the Huffman algorithm and its awesome page S&F Prod.