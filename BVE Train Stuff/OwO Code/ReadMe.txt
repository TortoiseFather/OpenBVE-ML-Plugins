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
10. Run OpenBVE-ML-Plugins\BVE Train Stuff\OwO Code\OwOvsBayes

Results will vary based upon input/datasize

IF YOU CANNOT GET THE PLUGIN TO WORK:
Run OpenBVE-ML-Plugins\BVE Train Stuff\OwO Code\OwOvsBayesTest