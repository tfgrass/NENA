# NENA
... baut avifs

## Local Development
### with docker
You can use `docker-compose up --build` to build, if you dont have dotnet installed. 

Change the `docker-compose.yml` to mount the processing folder (default is `testfolder`),

### with dotnet
With dotnet tools installed you can just `dotnet build` and `dotnet run`. Make sure to set `UPLOADS_PATH` 
to the folder you want to process. 
**Pro-Tip:** use `UPLOADS_PATH='to/your/path' dotnet run` to configure the path.