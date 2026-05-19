using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Project_F.Services;

public class FirebaseAuthService
{
    private readonly HttpClient _http = new HttpClient();

    private const string ApiKey = "AIzaSyCAFXoiAFlB5_uOVh1-kjkYJr27N60CUX8";
    private const string BaseUrl = "https://identitytoolkit.googleapis.com/v1/accounts";
    private const string FirestoreUrl = "https://firestore.googleapis.com/v1/projects/project-f-c6e4e/databases/(default)/documents";

    public async Task<(bool Success, string? IdToken, string? LocalId, string? Error)> SignInAsync(string email, string password)
    {
        var payload = new { email, password, returnSecureToken = true };
        var (success, data, error) = await PostAsync($"{BaseUrl}:signInWithPassword?key={ApiKey}", payload);

        if (!success || data == null) return (false, null, null, error);

        string? idToken = data.Value.GetProperty("idToken").GetString();
        string? localId = data.Value.GetProperty("localId").GetString();

        return (true, idToken, localId, null);
    }

    public async Task<(bool Success, string? Error)> SignUpAsync(string name, string email, string password)
    {
        var payload = new { email, password, returnSecureToken = true };
        var (success, data, error) = await PostAsync($"{BaseUrl}:signUp?key={ApiKey}", payload);

        if (!success || data == null) return (false, error);

        string? uid = data.Value.GetProperty("localId").GetString();
        string? idToken = data.Value.GetProperty("idToken").GetString();

        if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(idToken))
            return (false, "Invalid server response.");

        await SaveUserToFirestore(uid, name, email, idToken);

        return (true, null);
    }

    private async Task SaveUserToFirestore(string uid, string name, string email, string idToken)
    {
        var firestoreDoc = new
        {
            fields = new
            {
                name = new { stringValue = name },
                email = new { stringValue = email },
                createdAt = new { stringValue = DateTime.UtcNow.ToString("o") }
            }
        };

        var json = JsonSerializer.Serialize(firestoreDoc);

        var request = new HttpRequestMessage(HttpMethod.Patch,
            $"{FirestoreUrl}/users/{uid}?key={ApiKey}")
        {
            Content = new StringContent(json, Encoding.UTF8)
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

        await _http.SendAsync(request);
    }

    private async Task<(bool Success, JsonElement? Data, string? Error)> PostAsync(string url, object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var res = await _http.PostAsync(url, content);
        var resJson = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
        {
            try
            {
                var errDoc = JsonDocument.Parse(resJson);
                string firebaseError = errDoc.RootElement.GetProperty("error").GetProperty("message").GetString() ?? "UNKNOWN";
                return (false, null, firebaseError);
            }
            catch
            {
                return (false, null, "Something went wrong.");
            }
        }

        var doc = JsonDocument.Parse(resJson);
        return (true, doc.RootElement, null);
    }
}