using BeyondTrust.SecretSafeProvider.Services;
using BeyondTrust.SecretSafeProvider.Services.DataSources;
using BeyondTrust.SecretSafeProvider.Services.EphemeralResources;
using Imposter.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

[assembly: GenerateImposter(typeof(IDataSourceHandler))]
[assembly: GenerateImposter(typeof(IEphemeralResourceHandler))]
[assembly: GenerateImposter(typeof(IBeyondTrustApiFactory))]
[assembly: GenerateImposter(typeof(IBeyondTrustSecretSafe))]