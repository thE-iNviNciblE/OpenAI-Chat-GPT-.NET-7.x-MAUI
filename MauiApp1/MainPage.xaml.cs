namespace MauiApp1;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
//using System.Runtime.Remoting.Messaging;
using System.Net.Http;
//using System.Runtime.InteropServices.JavaScript.JSType;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.ObjectModel;

//using System.Collections.ObjectModel;

//using Foundation;
public class openai_models
{
    public string name { get; set; }
    public string id { get; set; }

}

public partial class MainPage : ContentPage
{
    public string OPENAI_API_KEY ="";
     SortedList oSortedList = new SortedList();
    public openai_models SelectedFruit { get; set; }

    ObservableCollection<openai_models> openai_model = new ObservableCollection<openai_models>();
    public ObservableCollection<openai_models> Fruits { get { return openai_model; } }


    private void txtOpenAIKey_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Zugriff auf das Textfeld-Objekt über den Parameter "sender"
        Editor txtBox = sender as Editor;

        // Überprüfung, ob der Cast erfolgreich war
        if (txtBox != null)
        {
            // Zugriff auf die Eigenschaften des Textfelds
            string text = txtBox.Text;

            // Verwendung der Eigenschaften des Textfelds
            if (text.Length > 0)
            {
                OPENAI_API_KEY = text;
                read_ai_models();
            }
        }
    }
    private void btnRefresh_Click(object sender , EventArgs e)
    {
        if (txtOpenAIKey.Text.Length > 0)
        {
            read_ai_models();
        } else
        {

            Application.Current.MainPage.DisplayAlert("Fehler!", "Kein API Schlüssel vorhanden", "OK");
        }
    }

    private void read_ai_models()
    {
        // cbModel.GetItemsAsArray();
        // cbModel.ItemsSource = oSortedList as IList;
        try
        {
            if (OPENAI_API_KEY.Length > 0)
            {

                Task.Run(async () =>
                {
                    // Now on background thread.

                    await getOpenAIModels2Combobox();

                    // cbModel.ItemsSource = gpt_models; // (BindingBase)(oSortedList;
                    // Report progress to UI.
                    Dispatcher.Dispatch(() =>
                    {
                        cbModel.ItemsSource = openai_model;
                        cbModel.SelectedIndex = 0;

                        //foreach (string keys in oSortedList.Values)
                        //{
                        //    cbModel.ItemsSource = cbModel.SetValue(keys);
                        //    cbModel.Items.Add("1" + keys);
                        //    //Console.WriteLine(keys);
                        //}
                    }
                    // Code here is queued to run on MainThread.
                    // Assuming you don't need to wait for the result,
                    // don't need await/async here.
                    );


                    // Still on background thread.

                });

            }
        }
        catch
        {
            Application.Current.MainPage.DisplayAlert("Fehler!", "Konnte AI Modelle nicht einlesen", "OK");
        }

    }


    public MainPage()
    {
        InitializeComponent();
        OPENAI_API_KEY = txtOpenAIKey.Text;
        cbModus.Items.Add("CHAT");
        cbModus.Items.Add("COMPLETION");
        cbModus.Items.Add("BILDER");
        cbModus.SelectedIndex = 0;

        read_ai_models();
    }
    
    private async void btnSend_Click(object sender, EventArgs e)
    {
        string sQuestion = txtQuestion.Text;
        if (sQuestion == "")
        {
            await Application.Current.MainPage.DisplayAlert("Genauigkeit", "Stelle eine Frage!", "OK");
            txtQuestion.Focus();
            return;
        }

        if (txtAnswer.Text != "")
            txtAnswer.Text += Constants.vbCrLf;

        txtAnswer.Text += ("Me: " + sQuestion + Constants.vbCrLf);
        txtQuestion.Text = "";

        try
        {
            string sAnswer = "";
            switch (cbModus.SelectedItem)
            {
                case "CHAT":
                    {
                        sAnswer = await SendMsg2Chat(sQuestion);
                        break;
                    }
                case "BILDER":
                    {
                        sAnswer = await SendMsg2Picture(sQuestion);
                        break;
                    }
                case "COMPLETION":
                    {
                        sAnswer = await SendMsg(sQuestion);
                        break;
                    }
            }

            txtAnswer.Text = txtAnswer.Text + Constants.vbCrLf + "Chat GPT: " + Strings.Replace(sAnswer, Constants.vbLf, Constants.vbCrLf);
            //SpeechToText(sAnswer);
        }
        catch (Exception ex)
        {
            txtAnswer.Text += ("Error: " + ex.Message);
        }
    }

    private async Task<string> SendMsg(string sQuestion)
    {
        int iMaxTokens = Convert.ToInt16(txtMaxTokens.Text); // 2048
        string sUserId = txtUserID.Text; // 1
        openai_models sModel = (openai_models)cbModel.SelectedItem; // text-davinci-002, text-davinci-003
        string json = "";
        double dTemperature = Convert.ToDouble(txtTemperature.Text); // 0.5

        if (dTemperature < 0 | dTemperature > 1)
        {
            await Application.Current.MainPage.DisplayAlert("Genauigkeit", "Zwischen 0 und 1", "OK");
            return "";
        }

        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls13 | System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls;

        using (HttpClient client = new HttpClient())
        { 
            json = "{";
            json += " \"model\":\"" + sModel.id + "\",";
            json += " \"prompt\": \"" + setQuoates(sQuestion) + "\",";
            json += " \"max_tokens\": " + iMaxTokens + ",";
            json += " \"user\": \"" + sUserId + "\", ";
            json += " \"temperature\": 0.3 ,"; //" + dTemperature + "
            json += " \"frequency_penalty\": 0.0, "; // Number between -2.0 and 2.0  Positive value decrease the model's likelihood to repeat the same line verbatim.
            json += " \"presence_penalty\": 0.0, "; // Number between -2.0 and 2.0. Positive values increase the model's likelihood to talk about new topics.
            json += " \"stop\": [\"#\", \";\"]"; // Up to 4 sequences where the API will stop generating further tokens. The returned text will not contain the stop sequence.
            json += "}";

            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + OPENAI_API_KEY);

            HttpResponseMessage response = await client.PostAsync("https://api.openai.com/v1/completions", content);
           // response.EnsureSuccessStatusCode();
            json = await response.Content.ReadAsStringAsync();
        }

        var myObj = JsonConvert.DeserializeObject<TextCompletion>(json);

        if (myObj.error == null)
        {
            string strMsg = "";
            for (int i = 0; i <= myObj.Choices.Count - 1; i++)
            {
                var firstModel = myObj.Choices[i];
                strMsg += firstModel.Text + " ";
            }
            return strMsg;
        }
        else
            try
            {
                return myObj.error.message;
            }
            catch (Exception ex)
            {
                return ex.Message.ToString();
            }
    }

    //public void SpeechToText(string s)
    //{
    //    if (chkMute.Checked)
    //        return;

    //    if (oSpeechSynthesizer == null)
    //    {
    //        oSpeechSynthesizer = new System.Speech.Synthesis.SpeechSynthesizer();
    //        oSpeechSynthesizer.SetOutputToDefaultAudioDevice();
    //    }

    //    if (cbVoice.Text != "")
    //        oSpeechSynthesizer.SelectVoice(cbVoice.Text);

    //    oSpeechSynthesizer.Speak(s);
    //}

    private async Task<string> SendMsg2Picture(string sQuestion)
    {
        //int iMaxTokens = Convert.ToInt16(txtMaxTokens.Text); // 2048
        //string sUserId = txtUserID.Text; // 1
        //openai_models sModel = (openai_models)cbModel.SelectedItem; // text-davinci-002, text-davinci-003
        string json;
        //double dTemperature = Convert.ToDouble(txtTemperature.Text); // 0.5

        //if (dTemperature < 0 | dTemperature > 1)
        //{
        //    await Application.Current.MainPage.DisplayAlert("Genauigkeit", "Zwischen 0 und 1", "OK");
        //    return "";
        //}

        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls13 | System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls;

        using (HttpClient client = new HttpClient())
        {
            string data = "{\"prompt\": \"" + sQuestion + "\",\"n\": 2,\"size\": \"1024x1024\",\"response_format\": \"url\"}";

            StringContent content = new StringContent(data, Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + OPENAI_API_KEY);

            HttpResponseMessage response = await client.PostAsync("https://api.openai.com/v1/images/generations", content);
            response.EnsureSuccessStatusCode();
            json = await response.Content.ReadAsStringAsync();
        }

        Dalle2 Dalle2 = JsonConvert.DeserializeObject<Dalle2>(json);
        if (Dalle2.error == null)
        {
            string strMsg = "";
            for (int i = 0; i <= Dalle2.data.Count - 1; i++)
                strMsg += Dalle2.data[i].url + Constants.vbCrLf + Constants.vbCrLf;
            return strMsg;
        }
        else
            return Dalle2.error.message;
    }

    private async Task<string> SendMsg2Chat(string sQuestion)
    {
        int iMaxTokens = Convert.ToInt32(txtMaxTokens.Text); // 2048
        string sUserId = txtUserID.Text; // 1
        openai_models sModel = (openai_models) cbModel.SelectedItem; // text-davinci-002, text-davinci-003
        string json;
        double dTemperature = Convert.ToDouble(txtTemperature.Text); // 0.5
        if (dTemperature < 0 | dTemperature > 1)
        {
            await Application.Current.MainPage.DisplayAlert("Genauigkeit", "Zwischen 0 und 1", "OK");
            return "";
        }

        if (txtQuestion.Text.Length == 0)
        {
            await Application.Current.MainPage.DisplayAlert("Fehler", "Keine Frage eingegeben", "OK");
            return "";
        }

        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls13 | System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls;

        using (HttpClient client = new HttpClient())
        {
            json = "{";
            json += " \"model\":\"" + sModel.id + "\",";
            json += " \"messages\" :[{\"role\": \"user\", \"content\": \"" + setQuoates(sQuestion) + "\"}],";
            json += " \"max_tokens\": " + iMaxTokens + ",";
            json += " \"user\": \"" + sUserId + "\", ";
            json += " \"temperature\": 0.3 ,"; //" + dTemperature + "
            json += " \"frequency_penalty\": 0.0" + ", ";
            json += " \"presence_penalty\": 0.0" + ", ";
            json += " \"stop\": [\"#\", \";\"]";
            json += "}";

            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + OPENAI_API_KEY);

            HttpResponseMessage response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
            //response.EnsureSuccessStatusCode();
            json = await response.Content.ReadAsStringAsync();
        }

        ChatRoot ChatObject1 = JsonConvert.DeserializeObject<ChatRoot>(json);

        if (ChatObject1.error == null)
        {
             
            // Zugriff auf Listen von Objekten
            foreach (var choice in ChatObject1.choices)
            {
                return choice.message.content;
            }
            return "";
        }
        else
            return ChatObject1.error.message;
    }
    public async Task<openai_models> getOpenAIModels2Combobox()
    {
        // https://beta.openai.com/docs/models/gpt-3
        string json;
        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls13 | System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls;

        using (HttpClient client = new HttpClient())
        {
            // Dim content As New StringContent(json, Encoding.UTF8, "application/json")
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + OPENAI_API_KEY);

            HttpResponseMessage response = await client.GetAsync("https://api.openai.com/v1/models");
            response.EnsureSuccessStatusCode();
            json = await response.Content.ReadAsStringAsync();
        }

        myModelsofGTP myObj = JsonConvert.DeserializeObject<myModelsofGTP>(json);

        if (myObj.error == null)
        {
            //  cbModel.Items.Clear  
            for (int i = 0; i <= myObj.Data.Count - 1; i++)
            {
                var firstModel = myObj.Data[i]; 
                openai_model.Add(new openai_models() { name = firstModel.Id + " - " + UnixToTime(firstModel.Created) + " by " + firstModel.Owned_by, id = firstModel.Id });
            }             
            return null;
        }
        else
        {
            txtAnswer.Text += "Chat GPT: " + Strings.Replace(myObj.error.message, Constants.vbLf, Constants.vbCrLf);
            //  SpeechToText(myObj.error.message);

            return null;
        }
    }
    public static DateTime UnixToTime(double unixTimeStamp)
    {
        // Unix timestamp is seconds past epoch
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
        return dateTime;
    }
    private string setQuoates(string s)
    {
        if (s.IndexOf(@"\") != -1)
            s = Strings.Replace(s, @"\", @"\\");

        if (s.IndexOf(Constants.vbCrLf) != -1)
            s = Strings.Replace(s, Constants.vbCrLf, @"\n");

        if (s.IndexOf(Constants.vbCr) != -1)
            s = Strings.Replace(s, Constants.vbCr, @"\r");

        if (s.IndexOf(Constants.vbLf) != -1)
            s = Strings.Replace(s, Constants.vbLf, @"\f");

        if (s.IndexOf(Constants.vbTab) != -1)
            s = Strings.Replace(s, Constants.vbTab, @"\t");

        if (s.IndexOf("\"") == -1)
            return s;
        else
            return Strings.Replace(s, "\"", @"\""");
    }

    private async void cbModeleLaden_CheckedChanged(object sender, EventArgs e)
    {
        cbModel.IsEnabled = false;

        await getOpenAIModels2Combobox();

        cbModel.IsEnabled = true;
    }

    private void BeendenToolStripMenuItem_Click(object sender, EventArgs e)
    {
        System.Environment.Exit(0);
    }

    private void APIKEYToolStripMenuItem_Click(object sender, EventArgs e)
    {
        //   frmAPIKEY frmapi = new frmAPIKEY();
        //    frmapi.ShowDialog(this);
    }

    private void OnCounterClicked(object sender, EventArgs e)
    {

    }
}


public class Dalle2_url
{
    public string url { get; set; }
}

public class Dalle2
{
    public int created { get; set; }
    public List<Dalle2_url> data { get; set; }

    public ResponseError error { get; set; }
}
public class ChatUsage
{
    public int prompt_tokens { get; set; }
    public int completion_tokens { get; set; }
    public int total_tokens { get; set; }
}

public class ChatMessage
{
    public string role { get; set; }
    public string content { get; set; }
}

public class ChatChoice
{
    public ChatMessage message { get; set; }
    public string finish_reason { get; set; }
    public int index { get; set; }
}

public class ChatRoot
{
    public string id { get; set; }
    public string @object { get; set; }
    public int created { get; set; }
    public string model { get; set; }
    public ChatUsage usage { get; set; }
    public List<ChatChoice> choices { get; set; }
    public ResponseError error { get; set; }
}
public class ResponseError
{
    [JsonProperty("message")]
    public string message { get; set; }
    [JsonProperty("type")]
    public string type { get; set; }
    [JsonProperty("param")]
    public object param { get; set; }
    [JsonProperty("code")]
    public object code { get; set; }
}

public class ResponseError_modelgpt
{
    [JsonProperty("message")]
    public string message { get; set; }
    [JsonProperty("type")]
    public string type { get; set; }
    [JsonProperty("param")]
    public object param { get; set; }
    [JsonProperty("code")]
    public object code { get; set; }
}

public class TextCompletion
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("object")]
    public string Object { get; set; }

    [JsonProperty("created")]
    public long Created { get; set; }

    [JsonProperty("model")]
    public string Model { get; set; }

    [JsonProperty("choices")]
    public List<Choice> Choices { get; set; }

    [JsonProperty("usage")]
    public Usage Usage { get; set; }
    [JsonProperty("[error]")]
    public ResponseError error { get; set; }
}

public class Choice
{
    [JsonProperty("text")]
    public string Text { get; set; }

    [JsonProperty("index")]
    public int Index { get; set; }

    [JsonProperty("logprobs")]
    public object Logprobs { get; set; }

    [JsonProperty("finish_reason")]
    public string FinishReason { get; set; }
}

public class Usage
{
    [JsonProperty("prompt_tokens")]
    public int prompt_tokens { get; set; }
    [JsonProperty("finish_reason")]
    public int finish_reason { get; set; }
    [JsonProperty("total_tokens")]
    public int total_tokens { get; set; }
}

public class myModelsofGTP
{
    [JsonProperty("[Object]")]
    public string Object { get; set; }
    [JsonProperty("Data")]
    public List<Gtpmodels> Data { get; set; }
    [JsonProperty("[error]")]
    public ResponseError_modelgpt error { get; set; }
}

public class Gtpmodels
{
    public string Id { get; set; }
    public string Object { get; set; }
    public long Created { get; set; }
    public string Owned_by { get; set; }
    public List<ModelPermission> Permission { get; set; }
    public string Root { get; set; }
    public object Parent { get; set; } // This can be a specific type if known.
}

public class ModelPermission
{
    public string Id { get; set; }
    public string Object { get; set; }
    public long Created { get; set; }
    public bool Allow_create_engine { get; set; }
    public bool Allow_sampling { get; set; }
    public bool Allow_logprobs { get; set; }
    public bool Allow_search_indices { get; set; }
    public bool Allow_view { get; set; }
    public bool Allow_fine_tuning { get; set; }
    public string Organization { get; set; }
    public object Group { get; set; } // This can be a specific type if known.
    public bool Is_blocking { get; set; }
}


