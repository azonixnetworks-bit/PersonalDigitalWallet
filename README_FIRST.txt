PERSONAL DIGITAL VAULT - FINAL VISUAL STUDIO SOLUTION

1. Extract this ZIP fully. Do NOT open the .sln from inside the ZIP preview.
2. Open: PersonalDigitalVault\PersonalDigitalVault.sln
3. Visual Studio should show exactly one project: PersonalDigitalVault.Api
4. The project file is physically at:
   backend\PersonalDigitalVault.Api\PersonalDigitalVault.Api.csproj
5. Frontend is inside the C# web project under wwwroot and is separated as:
   wwwroot\html
   wwwroot\css
   wwwroot\js
   wwwroot\assets
6. Install the .NET 8 SDK / Visual Studio ASP.NET and web development workload.
7. Restore NuGet packages when Visual Studio asks.
8. Update the SQL Server connection string in appsettings.json if your instance name is different.
9. Change JWT and AES development keys before any real deployment.

IMPORTANT: Secure uploaded user files are stored OUTSIDE wwwroot in SecureStorage.
