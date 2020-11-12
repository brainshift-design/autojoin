# autojoin
A small utility to join lists of files into a single file, with recursive includes and an option to watch for changes.

I am using this to develop Figma plugins, which requires the final code to be in one file. WebPack was overkill for me, so I made something minimal.

The syntax is

    autojoin list1.aj output1.html list2.aj output2.html -watch
 
You can add multiple pairs of list/output files, and set it to watch for changes in real time and automatically rejoin everything.

In a list file, you put one file per line. Files can be commented out with `//`, and other lists can be included recursively by putting them in square brackets.

In the following example, `[autojoin/operators.aj]` is another list file that gets expanded into the final output.

```
generate/generate_0.html

    util.js
    
    core/lib.js
    core/math.js
    core/random.js

    [autojoin/operators.aj]
   
    generate/generate.js

generate/generate_1.html
```
