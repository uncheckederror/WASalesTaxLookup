# WASalesTaxLookup
A standalone re-implementation of the WA DOR's WA Sale Tax Rate Lookup URL interface project.

# Why we built this
Getting the correct sale tax rate for ecommerce transactions in Washington State can be tricky because tax rates vary by municipality. Making this process as simple, fast, and reliable as possible is a good way to support a high-quality checkout experience.

# How we built this
The state published the source code for their current API on their website:
[Source code for the existing API](https://dor.wa.gov/taxes-rates/retail-sales-tax/destination-based-sales-tax-and-streamlined-sales-tax/washington-sales-tax-rate-library-source-code)

But this project was developed 13 years ago, as a Windows only app, and it contains a variety of functionality that isn't strictly necessary for the ecommerce use case. To leverage these opportunities for enhancement this project is a ground up re-implementation of the DOR's existing sales tax API. 

This is a .NET 5 Web API app that exposes a REST API with friendly Swagger documentation and OpenAPI 3 support. You can reuse your existing API integrations with the state's sales tax API, by simply switching in the base URL while retaining the specific HTTP route and parameters. This is because we have directly re-implemented the state's existing API to make to make switching to this stand-alone app as low-effort as possible. 

The data required to perform the sale tax rate lookups is downloaded from the [DOR's data download page](https://dor.wa.gov/taxes-rates/sales-and-use-tax-rates/downloadable-database) when you start the application up. Then its read into a SQLite 3 database, which is created and destroyed as needed, and lives in the root directory of the app. This first time startup process can take up to 10 minutes. Every time you start the app it performs a check to make sure that its data is current and in good health, if anything is off it re-ingests all the data from the state. Ideally you would restart the app once per quarter, on the first of the month, as that's when the new sales tax rates take effect.

Outside of this process to download data from the state and the requirement that you expose it to the internet through a webserver (nginx, IIS) this app has no external dependencies. Because .NET 5 is multiplatform this project offers support for Windows 10 and Linux. It may work on OS X, but this has not been tested.

# How to use this API
Read through our [Swagger docs](https://wataxlookup.acceleratenetworks.com/swagger/index.html) and make some test requests.

[Documentation on the DOR's existing API](https://dor.wa.gov/taxes-rates/retail-sales-tax/destination-based-sales-tax-and-streamlined-sales-tax/wa-sales-tax-rate-lookup-url-interface) which applies to this API as well.

# How to run this locally
* Clone this repo
* Install the [latest version of dotnet 5](https://dotnet.microsoft.com/download) on your system
* Open a shell in your local copy of this repo
* cd into the WASaleTax folder
* Execute the "dotnet run" command
* Use a browser navigate to the localhost URL provided in the shell window
* Optionally view the Swagger by adding "/swagger" to the localhost URL
