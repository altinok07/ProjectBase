@echo off
dotnet ef migrations remove --startup-project ../ProjectBase.Api/ --context ApplicationContext --force
pause
