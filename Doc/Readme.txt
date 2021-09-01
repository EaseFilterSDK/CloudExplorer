EaseFilter EaseTag Demo ReadMe

EaseTag demo is a c# Windows forms application, to demo the EaseTag Tiered Storage Filter Driver SDK. It will demo how to create a stub file which is pointing to local demo folder or pointing to cloud storage. 

How to run the EaseTag demo?

To test the local stub file:
1.	Run CloudConnect.exe, go to tools->create local stub test file.
2.	Choose a source folder as your test folder which has the test files.
3.	Choose a folder to store the stub files, click start, it will generate the stub files associated to the test files in the source folder.
4.	After the stub files were created, start the filter service. The stub files can be read as a regular file, when you open the stub file, all data will read from the source file by the demo application.
5.	For demo purpose, the local stub file’s reparse point tag is pointing to the source file in test folder.

To test the cloud stub file:
1. 	Run CloudConnect.exe, setup the cloud provider with the connection.
2. 	Run CloudConnect.exe, go to cloud explorer and choose the files, then click create stub file, choose a folder to store the stub files, 
	click start, it will generate the stub files with reparse tag data linking to the cloud remote path.
3. 	After the stub files were created, start the filter service. The stub files can be read as a regular file, when you open the stub file, the data from cloud storage will return back to the application.
