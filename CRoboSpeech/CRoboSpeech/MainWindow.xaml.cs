using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Threading;
using System.Text;
using Microsoft.CognitiveServices.SpeechRecognition;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace CRoboSpeech
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        BitmapImage m_bitmap = null;

        private MicrophoneRecognitionClient micClient;

        private SpeechRecognitionMode Mode
        {
            get
            {
                return SpeechRecognitionMode.LongDictation;
                //return SpeechRecognitionMode.ShortPhrase;
            }
        }

        private string DefaultLocale
        {
            get { return "ja-JP"; }
        }

        private string SpeechAuthenticationUri
        {
            get
            {
                return ConfigurationManager.AppSettings["SpeechAuthenticationUri"];
            }
        }

        private string SpeechSubscriptionKey
        {
            get
            {
                return ConfigurationManager.AppSettings["SpeechSubscriptionKey"];
            }
        }

        private Authentication auth = null;

        private string accessToken = string.Empty;

        private DeviceClient deviceClient = null;

        private int sendCount = 0;

        private string storageAccount = string.Empty;
        private string storageKey = string.Empty;
        private string storageContainer = string.Empty;

        private string pictureFilePath = string.Empty;
        private string uploadedFileName = string.Empty;

        private string voiceParam = "Microsoft Server Speech Text to Speech Voice (ja-JP, Ayumi, Apollo)";
        private CRoboSpeech.Gender voiceTypeParam = Gender.Female;
        private string localeParam = "ja-JP";


        public MainWindow()
        {
            InitializeComponent();

            textBoxDeviceId.Text = (string)Properties.Settings.Default["deviceId"];
            textBoxDeviceKey.Text = (string)Properties.Settings.Default["deviceKey"];
            this.storageAccount = (string)Properties.Settings.Default["storageAccount"];
            this.storageKey = (string)Properties.Settings.Default["storageKey"];
            this.storageContainer = (string)Properties.Settings.Default["storageContainer"];

            string checkText = string.Empty;
            if (textBoxDeviceId.Text != string.Empty)
            {
                checkText = textBoxDeviceId.Text.ToLower();
                if (checkText.IndexOf("trump") >= 0)
                {
                    voiceParam = "Microsoft Server Speech Text to Speech Voice (en-US, ZiraRUS)";
                    voiceTypeParam = Gender.Female;
                    localeParam = "en-US";
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (null != this.micClient)
            {
                this.micClient.Dispose();
            }

            base.OnClosed(e);
        }

        // メニュー - 開く
        private void miOpen_Click(object sender, RoutedEventArgs e)
        {
            // ファイルを開くダイアログ
            Microsoft.Win32.OpenFileDialog dlg =
                new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".jpg";
            dlg.Filter = "JPEG|*.jpg|BMP|*.bmp";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                pictureFilePath = dlg.FileName;

                // 既に読み込まれていたら解放する
                if (m_bitmap != null)
                {
                    m_bitmap = null;
                }
                // BitmapImageにファイルから画像を読み込む
                m_bitmap = new BitmapImage();
                m_bitmap.BeginInit();
                m_bitmap.UriSource = new Uri(pictureFilePath);
                m_bitmap.EndInit();
                // Imageコントロールに表示
                image1.Source = m_bitmap;
            }
        }

        // メニュー - 終了
        private void miExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // メニュー - 画面に合わせる
        private void miFit_Click(object sender, RoutedEventArgs e)
        {
            if (m_bitmap != null)   // 画像が読み込まれている場合
            {
                // スクロールバーを非表示
                scrollViewer1.VerticalScrollBarVisibility =
                    ScrollBarVisibility.Disabled;
                scrollViewer1.HorizontalScrollBarVisibility =
                    ScrollBarVisibility.Disabled;
                // Imageコントロールのサイズを
                // ScrollViewerのサイズに合わせる
                image1.Width = scrollViewer1.Width;
                image1.Height = scrollViewer1.Height;
            }
        }

        // メニュー - 等倍表示
        private void mi100_Click(object sender, RoutedEventArgs e)
        {
            if (m_bitmap != null)   // 画像が読み込まれている場合
            {
                // ScrollViewerのサイズよりImageのサイズ
                // の方が大きい場合はスクロールバー表示
                scrollViewer1.VerticalScrollBarVisibility =
                    ScrollBarVisibility.Auto;
                scrollViewer1.HorizontalScrollBarVisibility =
                    ScrollBarVisibility.Auto;
                // Imageのサイズを読み込んだ画像のサイズに合わせる
                image1.Width = m_bitmap.PixelWidth;
                image1.Height = m_bitmap.PixelHeight;
            }
        }

        // メニュー - 発声１
        private void miVoice1_Click(object sender, RoutedEventArgs e)
        {
            voiceParam = "Microsoft Server Speech Text to Speech Voice (ja-JP, Ayumi, Apollo)";
            voiceTypeParam = Gender.Female;
            localeParam = "ja-JP";
        }

        // メニュー - 発声２
        private void miVoice2_Click(object sender, RoutedEventArgs e)
        {
            voiceParam = "Microsoft Server Speech Text to Speech Voice (ja-JP, Ichiro, Apollo)";
            voiceTypeParam = Gender.Male;
            localeParam = "ja-JP";
        }

        // メニュー - 発声３
        private void miVoice3_Click(object sender, RoutedEventArgs e)
        {
            voiceParam = "Microsoft Server Speech Text to Speech Voice (en-US, ZiraRUS)";
            voiceTypeParam = Gender.Female;
            localeParam = "en-US";
        }

        private void buttonStartSpeech_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default["deviceId"] = textBoxDeviceId.Text;
            Properties.Settings.Default["deviceKey"] = textBoxDeviceKey.Text;
            Properties.Settings.Default.Save();

            Dispatcher.Invoke(() =>
            {
                buttonStartSpeech.IsEnabled = true;
            });

            if (this.micClient == null)
            {
                this.CreateMicrophoneRecoClient();
            }

            this.micClient.StartMicAndRecognition();

            SendMessageVisionInit();
        }

        /// <summary>
        /// Creates a new microphone reco client without LUIS intent support.
        /// </summary>
        private void CreateMicrophoneRecoClient()
        {
            this.micClient = SpeechRecognitionServiceFactory.CreateMicrophoneClient(
                this.Mode,
                localeParam,
                this.SpeechSubscriptionKey);
            this.micClient.AuthenticationUri = this.SpeechAuthenticationUri;

            // Event handlers for speech recognition results
            this.micClient.OnMicrophoneStatus += this.OnMicrophoneStatus;
            this.micClient.OnPartialResponseReceived += this.OnPartialResponseReceivedHandler;
            if (this.Mode == SpeechRecognitionMode.ShortPhrase)
            {
                this.micClient.OnResponseReceived += this.OnMicShortPhraseResponseReceivedHandler;
            }
            else if (this.Mode == SpeechRecognitionMode.LongDictation)
            {
                this.micClient.OnResponseReceived += this.OnMicDictationResponseReceivedHandler;
            }

            this.micClient.OnConversationError += this.OnConversationErrorHandler;
        }

        private void OnMicrophoneStatus(object sender, MicrophoneEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                WriteLine("--- Microphone status change received by OnMicrophoneStatus() ---");
                WriteLine("********* Microphone status: {0} *********", e.Recording);
                if (e.Recording)
                {
                    WriteLine("Please start speaking.");
                }

                WriteLine();
            });
        }

        private void OnMicShortPhraseResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            Dispatcher.Invoke((Action)(() =>
            {
                this.WriteLine("--- OnMicShortPhraseResponseReceivedHandler ---");

                // we got the final result, so it we can end the mic reco.  No need to do this
                // for dataReco, since we already called endAudio() on it as soon as we were done
                // sending all the data.
                this.micClient.EndMicAndRecognition();

                if (e.PhraseResponse.Results.Length > 0)
                    AnswerText(e.PhraseResponse.Results[0].DisplayText);

                this.WriteResponseResult(e);

                buttonStartSpeech.IsEnabled = true;
            }));
        }

        private void OnMicDictationResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            this.WriteLine("--- OnMicDictationResponseReceivedHandler ---");
            if (e.PhraseResponse.RecognitionStatus == RecognitionStatus.EndOfDictation ||
                e.PhraseResponse.RecognitionStatus == RecognitionStatus.DictationEndSilenceTimeout)
            {
                Dispatcher.Invoke(
                    (Action)(() =>
                    {
                        // we got the final result, so it we can end the mic reco.  No need to do this
                        // for dataReco, since we already called endAudio() on it as soon as we were done
                        // sending all the data.
                        this.micClient.EndMicAndRecognition();

                        buttonStartSpeech.IsEnabled = true;
                    }));
            }

            if (e.PhraseResponse.Results.Length > 0)
            {
                AnswerText(e.PhraseResponse.Results[0].DisplayText);
            }

            this.WriteResponseResult(e);
        }

        private void AnswerText(string message)
        {
            // Cognitive Services - Language Understanding Intelligent Service (LUIS) API not used because of this sample code being complicated
            if (localeParam == "en-US")
            {
                message = message.ToLower();
                if (message.IndexOf("hello") >= 0 && message.IndexOf("trump") >= 0)
                {
                    SayText("Hello, Mr. Doku. You look fine today.");
                }
                else if (message.IndexOf("thank") >= 0)
                {
                    SayText("You are welcome.");
                }
                else if (message.IndexOf("picture") >= 0 && message.IndexOf("explain") >= 0)
                {
                    AnalyzePhoto();
                }
                else if (message.IndexOf("picture") >= 0 && message.IndexOf("analyze") >= 0)
                {
                    AnalyzePhoto();
                }
                else if (message.IndexOf("photo") >= 0 && message.IndexOf("explain") >= 0)
                {
                    AnalyzePhoto();
                }
                else if (message.IndexOf("photo") >= 0 && message.IndexOf("analyze") >= 0)
                {
                    AnalyzePhoto();
                }
                else if (message.IndexOf("picture") >= 0 && message.IndexOf("trump") >= 0
                    && message.IndexOf("send") >= 0)
                {
                    SendMessageWithPhoto();
                }
                else if (message.IndexOf("photo") >= 0 && message.IndexOf("trump") >= 0
                    && message.IndexOf("send") >= 0)
                {
                    SendMessageWithPhoto();
                }
            }
            // このサンプルコードが複雑になる為、今回は、Cognitive Services - LUIS API(文章理解 API) は、使っていません。
            else
            {
                if (message.IndexOf("こんにちは") >= 0 && message.IndexOf("あゆみ") >= 0)
                {
                    SayText("こんにちは、ドクさん。お元気そうですね");
                }
                else if (message.IndexOf("ありがとう") >= 0)
                {
                    SayText("どういたしまして");
                }
                else if (message.IndexOf("写真") >= 0 && message.IndexOf("解説") >= 0)
                {
                    AnalyzePhoto();
                }
                else if (message.IndexOf("写真") >= 0 && message.IndexOf("解析") >= 0)
                {
                    AnalyzePhoto();
                }
                else if (message.IndexOf("写真") >= 0 && message.IndexOf("説明") >= 0)
                {
                    AnalyzePhoto();
                }
                else if (message.IndexOf("写真") >= 0 && message.IndexOf("トランプ") >= 0
                    && message.IndexOf("送って") >= 0)
                {
                    SendMessageWithPhoto();
                }
                else if (message.IndexOf("写真") >= 0 && message.IndexOf("トランプ") >= 0
                    && message.IndexOf("送信") >= 0)
                {
                    SendMessageWithPhoto();
                }
            }
        }

        private void AnalyzePhoto()
        {
            if (m_bitmap == null)
            {
                if (localeParam == "en-US")
                    SayText("There is no picture.");
                else
                    SayText("写真が表示されていないようです。");
                return;
            }
            if (localeParam == "en-US")
                SayText("I'm observing this picture closely, so please wait a moment.");
            else
                SayText("写真をじっくり観察していますので、少しお待ちください");

            uploadedFileName = UploadToBlob();

            SendMessageVisionAnalyze(CRoboType.CALL);
        }

        private void SendMessageWithPhoto()
        {
            if (m_bitmap == null)
            {
                if (localeParam == "en-US")
                    SayText("There is no picture.");
                else
                    SayText("写真が表示されていないようです。");
                return;
            }

            uploadedFileName = UploadToBlob();
            if (localeParam == "en-US")
                SayText("I'm sending this picture to Mr. Trump, so please wait a moment.");
            else
                SayText("かしこまりました。トランプさんにデータを送りますので、少々お待ちください。");
            SendMessageVisionAnalyze(CRoboType.D2D);
        }

        private void OnPartialResponseReceivedHandler(object sender, PartialSpeechResponseEventArgs e)
        {
            this.WriteLine("--- Partial result received by OnPartialResponseReceivedHandler() ---");
            this.WriteLine("{0}", e.PartialResult);
            this.WriteLine();
        }

        private void OnConversationErrorHandler(object sender, SpeechErrorEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                buttonStartSpeech.IsEnabled = true;
            });

            this.WriteLine("--- Error received by OnConversationErrorHandler() ---");
            this.WriteLine("Error code: {0}", e.SpeechErrorCode.ToString());
            this.WriteLine("Error text: {0}", e.SpeechErrorText);
            this.WriteLine();
        }

        private void WriteResponseResult(SpeechResponseEventArgs e)
        {
            if (e.PhraseResponse.Results.Length == 0)
            {
                this.WriteLine("No phrase response is available.");
            }
            else
            {
                this.WriteLine("********* Final n-BEST Results *********");
                for (int i = 0; i < e.PhraseResponse.Results.Length; i++)
                {
                    this.WriteLine(
                        "[{0}] Confidence={1}, Text=\"{2}\"",
                        i,
                        e.PhraseResponse.Results[i].Confidence,
                        e.PhraseResponse.Results[i].DisplayText);
                }

                this.WriteLine();
            }
        }

        private void WriteLine()
        {
            this.WriteLine(string.Empty);
        }

        private void WriteLine(string format, params object[] args)
        {
            var formattedStr = string.Format(format, args);
            Trace.WriteLine(formattedStr);
            Dispatcher.Invoke(() =>
            {
                textBoxSpeech.Text += (formattedStr + "\n");
                textBoxSpeech.ScrollToEnd();
            });
        }

        private void SayText(string speechText)
        {
            if (auth == null)
                auth = new Authentication();

            try
            {
                accessToken = auth.GetAccessToken();
            }
            catch (Exception ex)
            {
                WriteLine2("Failed authentication.");
                WriteLine2(ex.ToString());
                WriteLine2(ex.Message);
                return;
            }

            string requestUri = ConfigurationManager.AppSettings["SpeechRequestUri"];

            var cortana = new Synthesize();
            cortana.OnAudioAvailable += PlayAudio;
            cortana.OnError += ErrorHandler;

            WriteLine2("** Start Text to Speech");
            WriteLine2($"「{speechText}」");

            // Reuse Synthesize object to minimize latency
            cortana.Speak(CancellationToken.None, new Synthesize.InputOptions()
            {
                RequestUri = new Uri(requestUri),
                // Text to be spoken.
                Text = speechText,
                VoiceType = voiceTypeParam,
                // Refer to the documentation for complete list of supported locales.
                Locale = localeParam,
                // You can also customize the output voice. Refer to the documentation to view the different
                // voices that the TTS service can output.
                VoiceName = voiceParam,
                // Service can return audio in different output format.
                OutputFormat = AudioOutputFormat.Riff16Khz16BitMonoPcm,
                AuthorizationToken = "Bearer " + accessToken,
            }).Wait();
        }

        private void PlayAudio(object sender, GenericEventArgs<Stream> args)
        {
            //Console.WriteLine(args.EventData);

            // For SoundPlayer to be able to play the wav file, it has to be encoded in PCM.
            // Use output audio format AudioOutputFormat.Riff16Khz16BitMonoPcm to do that.
            SoundPlayer player = new SoundPlayer(args.EventData);
            player.PlaySync();
            args.EventData.Dispose();
        }

        private void ErrorHandler(object sender, GenericEventArgs<Exception> e)
        {
            WriteLine2("Unable to complete the TTS request: [{0}]", e.ToString());
        }

        private void WriteLine2()
        {
            this.WriteLine2(string.Empty);
        }

        private void WriteLine2(string format, params object[] args)
        {
            var formattedStr = string.Format(format, args);
            Trace.WriteLine(formattedStr);

            Dispatcher.Invoke(() =>
            {
                textBoxAnswer.Text += (formattedStr + "\n");
                textBoxAnswer.ScrollToEnd();
            });
        }

        private void buttonExitSpeech_Click(object sender, RoutedEventArgs e)
        {
            ExitSpeech();
        }

        private void ExitSpeech()
        {
            if (this.micClient != null)
            {
                this.micClient.EndMicAndRecognition();
                Dispatcher.Invoke(() =>
                {
                    buttonStartSpeech.IsEnabled = true;
                });
                this.micClient.AudioStop();
                this.micClient.Dispose();
            }
            this.micClient = null;
        }

        private void SendMessageVisionInit()
        {
            CRbHeader rbh = new CRbHeader();
            rbh.RoutingType = CRoboType.CALL;
            rbh.RoutingKeyword = CRoboType.Default;
            rbh.AppId = CRoboType.SbrApiServices;
            rbh.AppProcessingId = CRoboType.RbAppVisionApi;
            rbh.MessageId = CRoboType.Init;
            ++sendCount;
            rbh.MessageSeqno = sendCount.ToString();
            rbh.SendDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            CRbBodyBasic rbb = new CRbBodyBasic();
            rbb.visitor = "";

            CRoboMessage message = new CRoboMessage();
            message.RbHeader = rbh;
            message.RbBody = rbb;

            string strMessage = JsonConvert.SerializeObject(message);
            SendDeviceToCloudMessagesAsync(strMessage);
        }

        private void SendMessageVisionAnalyze(string routingType)
        {
            CRbHeader rbh = new CRbHeader();
            rbh.RoutingType = routingType;
            rbh.RoutingKeyword = CRoboType.Default;
            rbh.AppId = CRoboType.SbrApiServices;
            rbh.AppProcessingId = CRoboType.RbAppVisionApi;
            rbh.MessageId = CRoboType.Analyze;
            ++sendCount;
            rbh.MessageSeqno = sendCount.ToString();
            rbh.SendDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            CRbBodyVisionAnalyze rbb = new CRbBodyVisionAnalyze();
            rbb.visitor = "";
            rbb.visitor_id = "";
            rbb.blobFileName = uploadedFileName;
            if (routingType == CRoboType.D2D)
            {
                rbb.deleteFile = CRoboType.False;
                SendMessageBlobFileInfo();
            }
            else
            {
                rbb.deleteFile = CRoboType.True;
            }

            CRoboMessage message = new CRoboMessage();
            message.RbHeader = rbh;
            message.RbBody = rbb;

            string strMessage = JsonConvert.SerializeObject(message);
            SendDeviceToCloudMessagesAsync(strMessage);
        }

        private void SendMessageBlobFileInfo()
        {
            CRbHeader rbh = new CRbHeader();
            rbh.RoutingType = CRoboType.D2D;
            rbh.RoutingKeyword = CRoboType.Default;
            rbh.AppId = CRoboType.SbrApiServices;
            rbh.AppProcessingId = "";
            rbh.MessageId = CRoboType.BlobFileInfo;
            ++sendCount;
            rbh.MessageSeqno = sendCount.ToString();
            rbh.SendDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            CRbBodyBlobData rbb = new CRbBodyBlobData();
            rbb.blobFileName = this.uploadedFileName;
            rbb.storageAccount = this.storageAccount;
            rbb.storageKey = this.storageKey;
            rbb.storageContainer = this.storageContainer;

            CRoboMessage message = new CRoboMessage();
            message.RbHeader = rbh;
            message.RbBody = rbb;

            string strMessage = JsonConvert.SerializeObject(message);
            SendDeviceToCloudMessagesAsync(strMessage);
        }

        private async void SendDeviceToCloudMessagesAsync(string msg)
        {
            try
            {
                string iotHubHostName = ConfigurationManager.AppSettings["IotHubEndpoint"];

                if (deviceClient == null)
                {
                    deviceClient = DeviceClient.Create(iotHubHostName,
                        new DeviceAuthenticationWithRegistrySymmetricKey(textBoxDeviceId.Text, textBoxDeviceKey.Text));
                }

                var message = new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes(msg));

                await deviceClient.SendEventAsync(message);
            }
            catch(Exception ex)
            {
                MessageBox.Show("** Exception occured ** " + ex.ToString(), "Error");
            }
        }

        private async void ReceiveC2dAsync()
        {
            try
            {
                Properties.Settings.Default["deviceId"] = textBoxDeviceId.Text;
                Properties.Settings.Default["deviceKey"] = textBoxDeviceKey.Text;
                Properties.Settings.Default.Save();

                string iotHubHostName = ConfigurationManager.AppSettings["IotHubEndpoint"];
                string result;

                if (deviceClient == null)
                {
                    deviceClient = DeviceClient.Create(iotHubHostName,
                        new DeviceAuthenticationWithRegistrySymmetricKey(textBoxDeviceId.Text, textBoxDeviceKey.Text));
                }

                while (true)
                {
                    Microsoft.Azure.Devices.Client.Message receivedMessage = await deviceClient.ReceiveAsync();
                    if (receivedMessage == null)
                    {
                        continue;
                    }

                    result = Encoding.UTF8.GetString(receivedMessage.GetBytes());
                    await deviceClient.CompleteAsync(receivedMessage);

                    CheckMessage(result);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("** Exception occured ** " + ex.ToString(), "Error");
            }
        }

        private void CheckMessage(string msg)
        {
            var jo_resut = JsonConvert.DeserializeObject<JObject>(msg);
            var jo_rbh = (JObject)jo_resut["RbHeader"];
            var jo_rbb = (JObject)jo_resut["RbBody"];
            string routingType = (string)jo_rbh["RoutingType"];
            string appProcessingId = (string)jo_rbh["AppProcessingId"];
            string messageId = (string)jo_rbh["MessageId"];
            if (appProcessingId == CRoboType.RbAppVisionApi && messageId == CRoboType.Init)
            {
                this.storageAccount = (string)jo_rbb["storageAccount"];
                this.storageKey = (string)jo_rbb["storageKey"];
                this.storageContainer = (string)jo_rbb["storageContainer"];
                Properties.Settings.Default["storageAccount"] = this.storageAccount;
                Properties.Settings.Default["storageKey"] = this.storageKey;
                Properties.Settings.Default["storageContainer"] = this.storageContainer;
                Properties.Settings.Default.Save();
            }
            else if (routingType == CRoboType.D2D && messageId == CRoboType.BlobFileInfo)
            {
                DownloadBlobAndShow(jo_rbb);
            }
            else if (appProcessingId == CRoboType.RbAppVisionApi && messageId == CRoboType.Analyze)
            {
                string description;

                if (localeParam == "en-US")
                {
                    description = "This picture shows " + (string)jo_rbb["Description"] + ".";
                    SayText(description);

                    string tagText = "We can see ";
                    var tags = (JArray)jo_rbb["Tags"];
                    int cnt = 0;
                    foreach (JObject tag in tags)
                    {
                        tagText += (string)tag["name"];
                        if (cnt == (tags.Count - 1))
                        {
                        }
                        else if (cnt == (tags.Count - 2))
                        {
                            if (tags.Count >= 2)
                                tagText += " and ";
                        }
                        else
                        {
                            if (tags.Count >= 3)
                                tagText += ",";
                        }
                        ++cnt;
                    }
                    tagText += " in this picture.";
                    SayText(tagText);
                }
                else
                {
                    description = (string)jo_rbb["Description_jp"];
                    description += "という感じでしょうか";
                    SayText(description);

                    string tagText = "この写真の中には";
                    var tags = (JArray)jo_rbb["Tags"];
                    foreach (JObject tag in tags)
                    {
                        tagText += "、" + (string)tag["name_jp"];
                    }
                    tagText += "を見ることができます。";
                    SayText(tagText);
                }
            }
        }

        private void DownloadBlobAndShow(JObject jo_rbb)
        {
            try
            {
                string fileName = (string)jo_rbb["blobFileName"];
                string storageAccountParam = (string)jo_rbb["storageAccount"];
                string storageKeyParam = (string)jo_rbb["storageKey"];
                string storageContainerParam = (string)jo_rbb["storageContainer"];

                string storageConnectionString = "DefaultEndpointsProtocol=https;AccountName="
                                               + storageAccountParam + ";AccountKey=" + storageKeyParam;
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(storageContainerParam);
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);
                string filePath = Path.Combine(Environment.CurrentDirectory, fileName);

                blockBlob.DownloadToFile(filePath, FileMode.Create);

                pictureFilePath = filePath;

                // 既に読み込まれていたら解放する
                if (m_bitmap != null)
                {
                    m_bitmap = null;
                }
                // BitmapImageにファイルから画像を読み込む
                m_bitmap = new BitmapImage();
                m_bitmap.BeginInit();
                m_bitmap.UriSource = new Uri(pictureFilePath);
                m_bitmap.EndInit();
                // Imageコントロールに表示
                image1.Source = m_bitmap;
            }
            catch(Exception ex)
            {
                MessageBox.Show("** Exception occured ** " + ex.ToString(), "Error");
            }
        }

        private string UploadToBlob()
        {
            string newFileName = Path.GetFileNameWithoutExtension(pictureFilePath);

            try
            {
                string fileExtention = Path.GetExtension(pictureFilePath);

                newFileName += "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + fileExtention;

                string blobStorageConnString = "DefaultEndpointsProtocol=https;AccountName="
                    + this.storageAccount + ";AccountKey=" + this.storageKey;

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(blobStorageConnString);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(this.storageContainer);

                CloudBlockBlob blockBlob = container.GetBlockBlobReference(newFileName);
                using (var fileStream = File.OpenRead(pictureFilePath))
                {
                    blockBlob.UploadFromStream(fileStream);
                }

                return newFileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show("** Exception occured ** " + ex.ToString(), "Error");

                return null;
            }

        }

        private void buttonFinish_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            Environment.Exit(0);
        }

        private void buttonReceiveAsync_Click(object sender, RoutedEventArgs e)
        {
            ReceiveC2dAsync();
        }
    }
}
