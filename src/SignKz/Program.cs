using SignKz.Components;
using SignKz.Services;

var builder = WebApplication.CreateBuilder(args);

// Blazor Server (interactive). It uses SignalR inside, so updates reach all open tabs in real time.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

string contentRoot = builder.Environment.ContentRootPath;
string signsPath = Path.Combine(contentRoot, "Data", "signs.json");
string mediaPath = Path.Combine(contentRoot, "wwwroot", "media");
string progressPath = Path.Combine(contentRoot, "App_Data", "progress.json");

builder.Services.AddSingleton<ISignRepository>(new SignRepository(signsPath, mediaPath));
builder.Services.AddSingleton<IProgressStore>(new JsonProgressStore(progressPath));
builder.Services.AddSingleton<ProgressService>();
builder.Services.AddSingleton<IGestureRecognizer, GestureRecognizer>();
builder.Services.AddScoped<UserState>();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
