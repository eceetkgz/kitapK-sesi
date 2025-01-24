using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using KitapKosesi.Services;
using System;
using Google.Cloud.Firestore;

using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Servisleri kaydet
builder.Services.AddScoped<IFirebaseServisi, FirebaseServisi>();
builder.Services.AddScoped<IEPostaServisi, EPostaServisi>();

// Firebase Firestore bağlantısı
string projectId = builder.Configuration["Firebase:ProjectId"];
string credentialPath = builder.Configuration["Firebase:CredentialPath"];
Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialPath);
FirestoreDb firestoreDb = FirestoreDb.Create(projectId);
builder.Services.AddSingleton(firestoreDb);

// Servis kayıtları

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddRazorPages();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
});

// Kestrel yapılandırması
builder.WebHost.UseKestrel()
    .UseUrls("http://localhost:7138", "https://localhost:44300");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    // Development ortamında HTTPS yönlendirmesini devre dışı bırak
    app.UseHttpsRedirection();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors(); // CORS middleware'ini ekle

app.UseSession();
app.UseAuthentication(); // Kimlik doğrulama middleware'ini ekle
app.UseAuthorization();

// Varsayılan route'u Anasayfa controller'a yönlendir
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Anasayfa}/{action=Index}/{id?}");

app.Run();