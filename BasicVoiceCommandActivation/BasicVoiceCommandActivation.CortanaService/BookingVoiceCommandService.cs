using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Storage;

namespace BasicVoiceCommandActivation.CortanaService
{
    public sealed class BookingVoiceCommandService : IBackgroundTask
    {
	    private BackgroundTaskDeferral _serviceDeferral;
	    private VoiceCommandServiceConnection _cortanaConnection;

	    public async void Run(IBackgroundTaskInstance taskInstance)
	    {
		    _serviceDeferral = taskInstance.GetDeferral();

		    AppServiceTriggerDetails triggerDetail = taskInstance.TriggerDetails as AppServiceTriggerDetails;
		    if (triggerDetail?.Name == "BookingVoiceCommandService")
		    {
			    try
			    {
				    _cortanaConnection = VoiceCommandServiceConnection.FromAppServiceTriggerDetails(triggerDetail);
				    _cortanaConnection.VoiceCommandCompleted += ServiceConnectionOnVoiceCommandCompleted;

				    VoiceCommand command = await _cortanaConnection.GetVoiceCommandAsync();
				    if (command.CommandName == "bookTrain")
				    {
					    await SendMessageForBookTrain(command.Properties);
				    }
				    else
				    {
					    LaunchAppInForeground();
				    }
			    }
			    catch (Exception ex)
			    {
				    Debug.WriteLine("Exception " + ex);
			    }
			    finally
			    {
				    _serviceDeferral?.Complete();
			    }
		    }
		    else
		    {
				Debug.WriteLine("Not the right service");
			    _serviceDeferral?.Complete();
		    }
	    }

	    private async Task SendMessageForBookTrain(IReadOnlyDictionary<string, IReadOnlyList<string>> input)
	    {
		    try
		    {
			    string date = input["date"].FirstOrDefault();
			    string origin = input["origin"].FirstOrDefault();
			    string destination = input["destination"].FirstOrDefault();
				
			    List<VoiceCommandContentTile> resultTiles = new List<VoiceCommandContentTile>();

			    DateTime originDate = DateTime.Now.Date;
			    if (date == "demain")
			    {
				    originDate = DateTime.Now.AddMinutes(5);
			    }
			    else if (date == "tout à l'heure")
			    {
				    originDate = DateTime.Now.AddHours(2);
			    }
			    else if (date == "demain")
			    {
				    originDate = DateTime.Now.Date.AddDays(1).AddHours(8);
			    }

			    for (int i = 0; i < 3; ++i)
			    {
				    DateTime tripDate = originDate.AddHours(i);
				    VoiceCommandContentTile tile = new VoiceCommandContentTile()
				    {
					    ContentTileType = VoiceCommandContentTileType.TitleWith68x68IconAndText,
					    AppLaunchArgument = $"date={tripDate.ToString("yyyy-MM-dd_HH:mm:ss")}&destination={destination}&origin={origin}",
					    Title = $"{origin} => {destination}",
					    TextLine1 = $"Le {tripDate.ToString("dd/MM/yyyy")} à {tripDate.ToString("HH:mm")}",
					    TextLine2 = $"à partir de 42€",
					    TextLine3 = $"2ème classe",
					    Image = await StorageFile.GetFileFromApplicationUriAsync(new Uri(
						    "ms-appx:///Assets/bouh_68x68.jpg"
						    //"ms-appx:///Assets/bouh_68x92.jpg"
						    //"ms-appx:///Assets/bouh_280x140.png"
						    )),
						AppContext = new TrainInfo
						{
							Origin = origin,
							Destination = destination,
							Date = tripDate
						}
				    };

				    resultTiles.Add(tile);
			    }


				VoiceCommandUserMessage userMessage = new VoiceCommandUserMessage
				{
					DisplayMessage = $"Proposition de {origin} à {destination} {date}",
					SpokenMessage = $"Proposition pour {destination} {date}"
				};
				VoiceCommandResponse response = VoiceCommandResponse.CreateResponse(userMessage, resultTiles);
			    response.AppLaunchArgument = $"date={originDate.ToString("yyyy-MM-dd_HH:mm:ss")}&destination={destination}&origin={origin}";

				await _cortanaConnection.ReportProgressAsync(VoiceCommandResponse.CreateResponse(new VoiceCommandUserMessage()
				{
					SpokenMessage = "Recherche en cours",
					DisplayMessage = "Recherche de train en cours..."
				}));

				await Task.Delay(3000);

				VoiceCommandDisambiguationResult result = await _cortanaConnection.RequestDisambiguationAsync(VoiceCommandResponse.CreateResponseForPrompt(new VoiceCommandUserMessage
				{
					DisplayMessage = $"Proposition de {origin} à {destination} {date}",
					SpokenMessage = $"Proposition pour {destination} {date}"
				}, new VoiceCommandUserMessage
				{
					DisplayMessage = $"Disponibilités de {origin} à {destination} {date}",
					SpokenMessage = $"Choisissez votre voyage pour {destination} {date}"
				}, resultTiles));
			    if (result?.SelectedItem == null)
			    {
				    return;
			    }
				TrainInfo train = result.SelectedItem.AppContext as TrainInfo;

				VoiceCommandDisambiguationResult cbResult = await _cortanaConnection.RequestDisambiguationAsync(VoiceCommandResponse.CreateResponseForPrompt(new VoiceCommandUserMessage
				{
					SpokenMessage = "Choisissez votre prix",
					DisplayMessage = "Choisissez votre prix"
				}, new VoiceCommandUserMessage
				{
					SpokenMessage = "Choisissez le tarif que vous souhaitez pour le voyage",
					DisplayMessage = "Choisissez le tarif que vous souhaitez pour le voyage"
				}, TilesForPrices(train)));
				

			    if (cbResult != null)
			    {
				    
			    }

			    //VoiceCommandConfirmationResult confirmationResult = await _cortanaConnection.RequestConfirmationAsync(null);


				//await _cortanaConnection.ReportSuccessAsync(response);
		    }
		    catch (Exception e)
		    {
			    Debug.WriteLine("Exception " + e);
		    }
	    }

	    private IEnumerable<VoiceCommandContentTile> TilesForPrices(TrainInfo train)
	    {
			yield return new VoiceCommandContentTile
			{
				ContentTileType = VoiceCommandContentTileType.TitleWithText,
				AppLaunchArgument = $"date={train.Date.ToString("yyyy-MM-dd_HH:mm:ss")}&destination={train.Destination}&origin={train.Origin}",
				Title = $"{train.Origin} => {train.Destination} le {train.Date.ToString("dd/MM/yyyy")} à {train.Date.ToString("HH:mm")}",
				TextLine1 = $"2ème classe",
				TextLine2 = $"à 42€",
				AppContext = train.Clone(false)
			};
			yield return new VoiceCommandContentTile
			{
				ContentTileType = VoiceCommandContentTileType.TitleWithText,
				AppLaunchArgument = $"date={train.Date.ToString("yyyy-MM-dd_HH:mm:ss")}&destination={train.Destination}&origin={train.Origin}",
				Title = $"{train.Origin} => {train.Destination} le {train.Date.ToString("dd/MM/yyyy")} à {train.Date.ToString("HH:mm")}",
				TextLine1 = $"1ère classe",
				TextLine2 = $"à 73€",
				AppContext = train.Clone(true)
			};
		}

	    private void ServiceConnectionOnVoiceCommandCompleted(VoiceCommandServiceConnection sender, VoiceCommandCompletedEventArgs args)
	    {
		    _serviceDeferral?.Complete();
	    }

		private async void LaunchAppInForeground()
		{
			var userMessage = new VoiceCommandUserMessage {SpokenMessage = "Lancement de l'application"};

			var response = VoiceCommandResponse.CreateResponse(userMessage);
			
			await _cortanaConnection.RequestAppLaunchAsync(response);
		}

	    class TrainInfo
	    {
		    public string Origin { get; set; }

			public string Destination { get; set; }

			public DateTime Date { get; set; }

			public bool IsFirstClass { get; set; }

		    public TrainInfo Clone(bool isFirstClass)
		    {
			    return new TrainInfo
			    {
				    Origin = Origin,
				    Destination = Destination,
				    Date = Date,
				    IsFirstClass = isFirstClass
			    };
		    }
	    }
	}
}
