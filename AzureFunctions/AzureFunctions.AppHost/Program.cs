var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureFunctionsProject<Projects.AzureFunctionsApp>("azurefunctionsapp");

builder.AddAzureFunctionsProject<Projects.Durable FunctionsApp>("durable functionsapp");

builder.Build().Run();
