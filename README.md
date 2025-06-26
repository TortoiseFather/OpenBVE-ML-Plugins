# OpenBVE-ML-Plugins
A series of data collection listeners and ML processes for OpenBVE. I might make documentation if I'm not lazy.

OpenBVE https://openbve-project.net/ is an open source railway simulator.

These plugins are made for version 1.11.0.3.

They are built for the DMU Class 220 however have been tested on BR_C37-416 and Voyager-1, ll tested on the MF 1728 NCL-Berwick route.

Built with purpose of fitting SACRED methodology for purpose of Computer Science PhD. Also built for fun :)

Unfortunately I cannot share .train files or railway signal packages.

This project is unofficial and not endorsed by Christopher Lees. If this goes against the OpenBVE policy let me know and I'll take this down. I just found developing for InterfaceQuickReference a pain as I was unable to find documentation.

The 'documentation can be found within /examples folder' doesn't work, my copy of BVE did not come with an examples folder.

cl180 OPEN by Stephen Thomas (http://www.bve4trains.com/ www.pioneertrains.co.uk) reuploaded due to both domains being offline, if requested I will remove all references to the train. 

IMPORTANT NOTE: 
OpenBVE runs on an early version of C# and requires it's plugins to do so, as such the plugin that 
this code relies upon (OpenBVE-ML-Plugins\InputTracker) was designed in a way that
it saves to an external D: Drive. We are unable to guarentee the code will work on any external machine
Our attempts to recreate the code on machines other than our own were mixed.
Please note that some Windows systems prevent write access to C:\ without elevated permissions. 
Use a user-owned folder instead.
In event of failure, OpenBVE will display 'file not found', no files will save or the OwO results 
will be 0%


Steps for reproducing OwO code ->
1. Compile OpenBVE-ScoreAdder-Sourcecode\OpenBVE.sln
2. Launch OpenBve.exe (tested only on 64x)
3. Code tested on: Route Selection -> Browse Manually -> OpenBVE-ML-Plugins\BVE Train Stuff\LegacyContent\Railway\Route\Tyne Valley\[142]Su 1346 Swalwell Jct - Sunderland 0h 33m
4. Code tested on: Train Selection -> Browse Manually -> cl180 OPEN for Git
5. Code tested on: Mode of Driving: Normal
6. Start
7. F10 to show stats
8. F to start engine, Z to increase speed, A to decrease speed
9. Results are saved to "D:\OpenBVE_Data\OpenBVE_Train_Data\train_data_log.csv"
10. Open OpenBVE-ML-Plugins\BVE Train Stuff\OwO Code\OwOvsBayes
11. Replace train_data_log.csv with your own code.
12. Run OwOvsBayes

Results will vary based upon input/datasize

IF YOU CANNOT GET THE PLUGIN TO WORK:
Run OpenBVE-ML-Plugins\BVE Train Stuff\OwO Code\OwOvsBayes

IF YOU CANNOT GET THE CODE TO WORK:
Evidence of it working can be found at OpenBVE-ML-Plugins\BVE Train Stuff\OwO Code\OwOvsBayes\results.png