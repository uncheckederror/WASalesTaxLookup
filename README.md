# WASalesTaxLookup
A re-implementation of the WA DOR's WA Sale Tax Rate Lookup URL interface project.

# Why we built this
Getting the correct sale tax rate for ecommerce transactions in Washington State can be tricky because tax rates vary by municipality. Making this process as simple, fast, and reliable as possible is a good way to support a high-quality checkout experience.

# How we built this
The state published the source code for their current API on their website:
[Source code for the existing API](https://dor.wa.gov/washington-sales-tax-rate-library-source-code)

But this project was developed 14 years ago, as a Windows only app, and it contains a variety of functionality that isn't strictly necessary for the ecommerce use case. To leverage these opportunities for enhancement this project is a ground up re-implementation of the DOR's existing sales tax API. 

This is a .NET 9 Web API app that exposes a REST API with friendly Scalar documentation and OpenAPI 3 support. You can reuse your existing API integrations with the state's sales tax API, by simply switching in the base URL while retaining the specific HTTP route and parameters. This is because we have directly re-implemented the state's existing API to make to make switching to this stand-alone app as low-effort as possible.

Alternatively the /GetTaxRate endpoint will respond with a simplifed JSON payload containing the tax rate you need to complete your checkout process.

The data required to perform the sale tax rate lookups is downloaded from the [DOR's data download page](https://dor.wa.gov/taxes-rates/sales-and-use-tax-rates/downloadable-database) when you start the application up. Every time you start the app it re-ingests all the data for the current quarter from the state, which takes a few seconds. Ideally you would redeploy the app once per quarter, on the first of the month, as that's when the new sales tax rates take effect.

Because .NET 9 is multiplatform this project offers support for Windows and Linux. It may work on OS X, but this has not been tested.

[Read more about building this project here.](https://thomasryan.dev/2025/01/wa-sales-tax-lookup-api-new-year.html)

# How to use this API
Read through our [API docs](https://wataxlookup.acceleratenetworks.com/scalar/v1) and make some test requests.

[Documentation on the DOR's existing API]([https://dor.wa.gov/taxes-rates/retail-sales-tax/destination-based-sales-tax-and-streamlined-sales-tax/wa-sales-tax-rate-lookup-url-interface) which applies to this API as well.

# How to run this locally
* Clone this repo
* Install the [latest version of .NET 9](https://dotnet.microsoft.com/download) on your system
* Open a shell in your local copy of this repo
* cd into the WASaleTax folder
* Execute the "dotnet run" command
* Use a browser navigate to the localhost URL provided in the shell window
* Optionally view the docs by adding "/swagger" to the localhost URL

# Troubleshooting
All of the data the app requires is ingested on start up. You can verify that everything is working correctly by executing a test request from the API docs. 
