CloudFile SDK ReadMe

CloudFile SDK includes the the binary folder "bin" which has the driver "cloudfile.sys" and API DLL "FilterAPI.dll" for different platform.
It also includes the demo projects with the source code in C++ and C#.

1. CloudFileCSDemo is simple C# application, to demo how to use the EaseFilter Cloud File Sytem SDK. 
	To run the demo application as the Windows service with below command in dos prompt:
	CloudFileCSDemo -installservice

	To run the demo application as the console app with below command in dos prompt:
	CloudFileCSDemo.exe -console

	a. It will generate a test cloud folder "TestCloudFolder" in the application folder, if you want to have more test cloud files, you can put your test files to the "TestSourceFolder".       
	b. You can browse the "TestCloudFolder" and read the virtual files there, the virtual file just like the regular physical files in local.
	c. For demo purpose, when you read the cloud file, it will retrieve the file data from the "TestSourceFolder", you can change it to your remote sever, or in the cloud in code.

2. CloudFileS3Demo is a C# Windows Form application, to demonstrate how to integrate AWS S3 files to the local file system. 
   In this project, you can map the local folder with the AWS S3 folder, then you can access the cloud files in the mapped folder as the local physical files.

3. CloudFileCPPDemo is a C++ project, it will demo how to use the EaseFilter Cloud File System SDK in C++.
