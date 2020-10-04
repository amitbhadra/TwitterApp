The members of this team - 
    Amit Asish Bhadra - 66494087
    Rishab Aryan Das  - 05369273


1. Optimum work unit is choosing 12 number of actors because my number of cores is 12 and choosing anything more or less causes the concurrency to decrease. This ratio is calculated over many number of actors and the best possible one is 12 for me. I have noticed that when the input size is too low, I'm getting low concurrency, the beauty of parallelism shows only when the input N and k is increased a lot.

2. Please run the code as -
dotnet fsi --langversion:preview program.fsx <N> <k>
There is no result for 10^6 and 4. 

However, the result for 10^8 and 24 is as below -- 
9
1
20
44
76
25
304
197
121
353
540
1301
2053
3597
856
3112
5448
8576
12981
20425
30908
35709
54032
84996
128601
202289
306060
353585
534964
841476
1273121
2002557
3029784
3500233
5295700
8329856
12602701
19823373
22579140
58980291
82457176
90916537
Real: 00:02:53.857, CPU: 00:21:52.822, GC gen0: 30363, gen1: 7383, gen2: 18

For 10^6 and 24 it is --
1
9
20
44
197
353
25
121
76
304
540
1301
856
2053
3112
3597
5448
8576
12981
20425
30908
35709
54032
84996
128601
202289
306060
353585
534964
841476
Real: 00:00:01.177, CPU: 00:00:12.248, GC gen0: 303, gen1: 70, gen2: 1

For 10^8 and 20 it is --
53553387
62780852
88700958
Real: 00:02:42.922, CPU: 00:21:25.719, GC gen0: 28302, gen1: 6837, gen2: 7


3. The maximum concurrency I have got is 10.4. For values where number of actors are around 500-1000, I'm getting a concurrency of 6-7

4. The largest problem I managed to solve is 10^8