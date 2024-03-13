﻿using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using (var httpClient = new HttpClient())
{
    httpClient.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("95b73796654d46f5a80815866d58f40b");

}

 static async Task<string> UploadFileAsync(string filePath, HttpClient httpClient)
{
    using (var fileStream = File.OpenRead(filePath))
    using (var fileContent = new StreamContent(fileStream))
    {
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using (var response = await httpClient.PostAsync("https://api.assemblyai.com/v2/upload", fileContent))
        {
            response.EnsureSuccessStatusCode();
            var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>();
            return jsonDoc.RootElement.GetProperty("upload_url").GetString();
        }
    }
}

 static async Task<Transcript> CreateTranscriptAsync(string audioUrl, HttpClient httpClient)
{
    var data = new { audio_url = audioUrl };
    var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

    using (var response = await httpClient.PostAsync("https://api.assemblyai.com/v2/transcript", content))
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Transcript>();
    }
}



 static async Task<Transcript> WaitForTranscriptToProcess(Transcript transcript, HttpClient httpClient)
{
    var pollingEndpoint = $"https://api.assemblyai.com/v2/transcript/{transcript.Id}";

    while (true)
    {
        var pollingResponse = await httpClient.GetAsync(pollingEndpoint);
        transcript = await pollingResponse.Content.ReadFromJsonAsync<Transcript>();
        switch (transcript.Status)
        {
            case "processing":
            case "queued":
                await Task.Delay(TimeSpan.FromSeconds(3));
                break;
            case "completed":
                return transcript;
            case "error":
                throw new Exception($"Transcription failed: {transcript.Error}");
            default:
                throw new Exception("This code shouldn't be reachable.");
        }
    }
}
using (var httpClient = new HttpClient())
{
    httpClient.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("95b73796654d46f5a80815866d58f40b");

    var uploadUrl = await UploadFileAsync(@"C:\Users\Hamid\TestFolder\test1.MP3", httpClient);
    var transcript = await CreateTranscriptAsync(uploadUrl, httpClient);
    transcript = await WaitForTranscriptToProcess(transcript, httpClient);

    Console.WriteLine(transcript.Text);
}
public class Transcript
{
    public string Id { get; set; }
    public string Status { get; set; }
    public string Text { get; set; }

    [JsonPropertyName("language_code")]
    public string LanguageCode { get; set; }

    public string Error { get; set; }
}


