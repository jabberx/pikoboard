# pikoboard
pikoboard if fat-free nanoboard (distributed imageboard).

By fat-free I mean it lacks all that crazy stuff that was added to nanoboard making it overburdened and not very effective. 

Nanoboard retranslation is poor due to image embedding and PNG-LSB containers huge size and small capacity. Nanoboard JS features and user interface make the code complex and ugly. PNG-LSB and posts packing algorithm in Nanoboard is also extremely insane and hard to reproduce. 

They removed thread hash from a post forcing any developer to dive into recursive shit, making programs slow and complex. 

They added BitMessage transport which requires users online, is way too expensive for the CPU and gives only 28 days max life for the post (compared to relatively long life of slow imageboard threads), also it forces two http daemons to be running speaking with each other using JSON API - double crazyness.

I am the original nanoboard ideator (and first client (early 2014) creator). Back then we had thread hashes in each post, external (not embedded) images. As for containers there was no decisions ready, I considered PNG-7z containers but did not included them, I was forced to leave the project due to life circumstances. 

Now I see that nanoboard is back but damaged, cursed and fucked up. I want to resurrect my original vision in a new project but cannot use Nanoboard name anymore because it was blackened. I would call it pikoboard.

To be honest I borrowed some code from Nanoboard (aggregator - named it crawler, nano.css file).

This repo holds a simple tool with simple responsibilities. It's not so 'cool' as present nanoboard 'clients' but at least you understand what's going on underneath. You can even play with this tool solely from your console. Many operations can be more automatized but that is not so important at such early stage. You can automatize it by yourself if you're experienced enough with shell scripts (or you can add some javascript if you like). It is as small as possible and yet brings pikoboard to the life and makes it fully usable. I will add bootstrap thread urls into places.txt soon (need to test code for bugs first).

## Key differences from nanoboard
* no js at all (yet), html/css only
* no daemon (http server) – just pregenerated static html pages
* no (own or any) database – just folders and files and you have full manual control of them
* gzip-jpegs instead of PNG-LSB
* post size limit - 5k characters
* images (and also files) referenced in posts by their hash (not embedded) and transferred separately: usually others do not retranslate your images or files unless their size is really small (currently 1536 bytes max) while you transfer your files once when added
* obvious: crawler downloads jpegs not PNGs, jpeg_containers should contain jpegs
* whole thread goes into gzip-jpeg container (or several containers if thread is too big) each time you feed the post to the pikoboard.exe
* there is no limits for your images and files, but gzip-jpegs have size limit so if you need to transfer several big files (like 5Mb each) - several gzip-jpegs will be created; but you should expect that not every imageboard will accept such big jpeg
* to answer or create thread you just write in a text file:
```
thread=hash, empty for new thread
message
[ref=hash] 
^ image
[raw=hash] - file
>>hash - post ref (on page)
>>>hash - thread link
[b]bb [u]codes[/u] [s]also[/s] [spoiler]work[/spoiler] [i]here[/i][/b]
```
and pass this file to the pikoboard.exe either by dragging a file to the .exe (Windows) or writing:
```
mono pikoboard.exe post.txt (Unix)
---
pikoboard post.txt (Win)
```
in command line.
* to include image you just do with it the same you did for the post.txt – hash for it will be calculated, image will be added to your 'database' and new post.txt will appear with template referencing that image inside
* there's no built-in categories and no recursive threads, users should organize themselves, for example: create thread for categories LISTING and for EACH category separately; in a listing you MENTION some category thread (>>>); in some category thread you also mention some real thread that you want to include into this particular category (also >>>)
```
CATEGORIES THREAD EXAMPLE:
f3..12
Categories
–––––––––––––––
 ee..34
 /b/ >>>cd..3f
 –––––––––––––––
 a1..1e
 /au/ >>>3e..05
 –––––––––––––––

/b/ THREADS LISTING EXAMPLE:
cd..3f
/b/
––––––––––––
  a5..12
  thread #1: >>>31..bd
  –––––––––––––––––––
  f2..28
  other thread: >>>44..d9
  ––––––––––––––––––––––
```

## How to build
* Press 'Download Zip' button
* Compile using csc.exe (on windows) or dmcs (on unix-like OS).
* Search internet to know where to find csc.exe on your Windows machine. Usually it will be something like:
```
C:\Windows\Microsoft.NET\Framework\v*.*.*\csc.exe pikoboard.cs
```

## How to use

Real working session log:
```
$ ls -l jpeg_containers/
 512604 Jan 31 00:42 test.jpg
$ cat >post.txt
thread=
this is my new test thread
$ mono pikoboard.exe post.txt 
$ ls -l for_upload/
 512662 Mar  6 21:40 upload4d7bc82d99ae36d05e28e6654489713e_1.jpg
$ ls html
4d7bc82d99ae36d05e28e6654489713e.html
$ open html/4d7bc82d99ae36d05e28e6654489713e.html 
$ cat >post.txt
thread=4d7bc82d99ae36d05e28e6654489713e
test reply
$ mono pikoboard.exe post.txt 
$ ls -l for_upload/
 512705 Mar  6 21:48 upload2bddc2a2dbfe1fa0da7556eed793e023_1.jpg
 512662 Mar  6 21:40 upload4d7bc82d99ae36d05e28e6654489713e_1.jpg
$ mono pikoboard.exe -a
Running crawler...
Finished downloading.
Checking new images...
Cleaning up...
Done!
$ cat places.txt 
# put urls to threads here, each at new line:
$ ls board_files/
$ mono pikoboard.exe ~/Desktop/chloe_pic.jpg 
$ cat post.txt 
thread=enter hash of thread here or just leave thread= for new thread
[ref=99ff1657c237dc1e2f4378111d05cd5f]
$ ls board_files/
99ff1657c237dc1e2f4378111d05cd5f
$ diff ~/Desktop/chloe_pic.jpg board_files/99ff1657c237dc1e2f4378111d05cd5f 
$ vi post.txt
$ cat post.txt
thread=4d7bc82d99ae36d05e28e6654489713e
>>2bddc2a2dbfe1fa0da7556eed793e023
[ref=99ff1657c237dc1e2f4378111d05cd5f]
hi rate my girlfriend ^^
$ mono pikoboard.exe post.txt 
$ open html/4d7bc82d99ae36d05e28e6654489713e.html
$ ls -l for_upload/
 512705 Mar  6 21:48 upload2bddc2a2dbfe1fa0da7556eed793e023_1.jpg
 512662 Mar  6 21:40 upload4d7bc82d99ae36d05e28e6654489713e_1.jpg
 543706 Mar  6 22:21 upload5a153c7322bf7563f44d4cbb0d6607e8_1.jpg
 ```

## Other tricks
* To delete some thread obvious steps would be: go to database directory, find directory name same as hash of a thread you want to delete and delete this directory
* But you will receive it again after next pikoboard.exe -a operation if someone will make new post to that thread. To 'ban' this thread forever you should call pikoboard.exe -a from a batch/shell script and after this call you add lines that remove unwanted thread directory recursively, example:
```
mono pikoboard.exe -a
rm -r database/some_unwanted_thread_hash
rm -r html/some_unwnated_thread_hash
```
* You can do same thing to the particular posts in database dir but they will show up in html anyway because html is refreshed within -a operation, so you need to refresh thread after removal of some posts, you can do it with -r option:
```
mono pikoboard.exe -a
...
rm -r database/thread_hash/unwanted_post_hash
pikoboard -r thread_hash
```
* Retranslating some post (and its images and files what's important) again (for example if you lost containers for it). Locate post file in database, copy to some txt file (post.txt is fine), you will see that it starts from hash (thread hash), add thread= to the beginning, add new line after hash, save. You have recreated your post.txt, now pass it to the pikoboard.exe and you will receive your container(s).
