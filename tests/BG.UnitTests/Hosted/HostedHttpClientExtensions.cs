using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.StaticFiles;

namespace BG.UnitTests.Hosted;

internal static partial class HostedHttpClientExtensions
{
    private const string AntiForgeryFieldName = "__RequestVerificationToken";

    public static async Task<HttpResponseMessage> PostFormWithAntiforgeryAsync(
        this HttpClient client,
        string getPath,
        string postPath,
        IEnumerable<KeyValuePair<string, string?>> formValues)
    {
        var token = await client.GetAntiforgeryTokenAsync(getPath);
        var payload = formValues
            .Where(entry => entry.Value is not null)
            .ToDictionary(entry => entry.Key, entry => entry.Value!);
        payload[AntiForgeryFieldName] = token;

        using var request = new HttpRequestMessage(HttpMethod.Post, postPath)
        {
            Content = new FormUrlEncodedContent(payload)
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        return await client.SendAsync(request);
    }

    public static async Task<HttpResponseMessage> PostMultipartWithAntiforgeryAsync(
        this HttpClient client,
        string getPath,
        string postPath,
        IEnumerable<KeyValuePair<string, string?>> formValues,
        string fileFieldName,
        string fileName,
        byte[] fileBytes)
    {
        var token = await client.GetAntiforgeryTokenAsync(getPath);

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(token), AntiForgeryFieldName);

        foreach (var entry in formValues.Where(entry => entry.Value is not null))
        {
            content.Add(new StringContent(entry.Value!), entry.Key);
        }

        var provider = new FileExtensionContentTypeProvider();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            provider.TryGetContentType(fileName, out var contentType)
                ? contentType
                : "application/octet-stream");
        content.Add(fileContent, fileFieldName, fileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, postPath)
        {
            Content = content
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        return await client.SendAsync(request);
    }

    public static async Task<string> GetAntiforgeryTokenAsync(this HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        var match = AntiForgeryTokenRegex().Match(html);

        if (!match.Success)
        {
            throw new InvalidOperationException($"Unable to find antiforgery token on '{path}'.");
        }

        return match.Groups["token"].Value;
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AntiForgeryTokenRegex();
}
