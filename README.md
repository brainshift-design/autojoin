# autojoin
A small utility to join lists of files into a single file.

You can add multiple pairs of list/output files, and set it to watch for changes in real time and automatically rejoin everything.

The syntax is

    autojoin list1.aj output1.html list2.aj output2.html -watch
 
In a list file, you put one file per line. Files can be commented out with `//`, and other lists can be included recursively by putting them in square brackets.

In the following example, `[autojoin/otherlist.aj]` is another list file that gets expanded into the final output.

```
output/output_0.html

    util.js
    
    core/lib.js
    core/math.js
    core/random.js

    [autojoin/otherlist.aj]
   
    folder/*.js

output/output_1.html
```

I am using this to develop Figma plugins, which requires the final code to be in one file. WebPack seemed like overkill, so I made something minimal.
