using ImageMagick;
using Telegram.Bot;
using Telegram.Bot.Types;

var client = new TelegramBotClient(" ");
client.StartReceiving(Update, Error);

async Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
{
    var message = update.Message;

    if (message.Text != null)
    {
        //Console.WriteLine($"Username: {message.Chat.Username} | {message.Text}");
        await botClient.SendTextMessageAsync(message.Chat.Id, "Please, send me a '.HEIC' photo as a document");
        return;
    }

    if (message.Photo != null)
    {
        //if (message.Text.Contains("Hi"))
        {
            await botClient.SendTextMessageAsync(message.Chat.Id, "I need your photo to be sent as a document to convert it");
            return;
        }
    }

    if (message.Document != null)
    {
        string FolderPath = "A:\\TempFolder";

        var fileId = update.Message.Document.FileId;
        var fileInfo = await botClient.GetFileAsync(fileId);
        var filePath = fileInfo.FilePath;

        string FileExtension = Path.GetExtension(filePath);

        if (FileExtension.ToLower() == ".heic")
        {
            if (Directory.Exists(FolderPath))
            {
                Directory.Delete(FolderPath, true);
            }
            Directory.CreateDirectory(FolderPath);

            await botClient.SendTextMessageAsync(message.Chat.Id, "Your photo(s) is(are) being processed. This may take a few seconds..");

            string destinationFullFilePath = $@"{FolderPath}\{message.Document.FileName}";
            await using Stream fileStream = System.IO.File.Create(destinationFullFilePath);
            await botClient.DownloadFileAsync(filePath, fileStream);

            fileStream.Close();

            string[] allfiles = Directory.GetFiles($"{FolderPath}", "*.heic");

            foreach (var file in allfiles)
            {
                FileInfo info = new FileInfo(file);
                using (MagickImage image = new MagickImage(info.FullName))
                {
                    // Save frame as jpg
                    var newFileName = info.Name.ToLower().Replace(".heic", "(converted).jpg");
                    image.Write(@$"{FolderPath}\{newFileName}");

                    string convertedFilePath = $@"{FolderPath}\{newFileName}";

                    await using Stream stream = System.IO.File.OpenRead(convertedFilePath);
                    {
                        await botClient.SendPhotoAsync(message.Chat.Id, InputFile.FromStream(stream, convertedFilePath));
                        stream.Seek(0, SeekOrigin.Begin); // Reset stream position to the beginning
                        stream.Position = 0;
                        await botClient.SendDocumentAsync(message.Chat.Id, InputFile.FromStream(stream, newFileName));
                    } // 'stream' will be automatically closed here
                }
            }

            await botClient.SendTextMessageAsync(message.Chat.Id, "Your photo was sucessfully converted!");
            return;
        }

        else
        {
            await botClient.SendTextMessageAsync(message.Chat.Id, $"You've sent me the '{FileExtension}' file. Please send me a '.HEIC' photo as a file");
            return;
        }


    }

}

async Task Error(ITelegramBotClient client, Exception exception, CancellationToken token)
{
    throw new NotImplementedException();
}



Console.ReadLine();
