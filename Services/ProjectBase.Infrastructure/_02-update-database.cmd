set /p id=Ortam:
set ASPNETCORE_ENVIRONMENT=%id%
dotnet ef --startup-project ../ProjectBase.Api/ database update --context ApplicationContext
pause