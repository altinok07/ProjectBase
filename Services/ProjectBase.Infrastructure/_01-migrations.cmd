set /p name=MigrationName:
dotnet ef migrations --startup-project ../ProjectBase.Api/ add V_%name% --context ApplicationContext
pause
