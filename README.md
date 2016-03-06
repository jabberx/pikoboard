# pikoboard
pikoboard if fat-free nanoboard (distributed forum)

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
