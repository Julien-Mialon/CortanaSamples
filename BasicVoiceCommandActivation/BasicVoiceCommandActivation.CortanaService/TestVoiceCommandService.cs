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
    public sealed class TestVoiceCommandService : IBackgroundTask
    {
	    private BackgroundTaskDeferral _serviceDeferral;
	    private VoiceCommandServiceConnection _cortanaConnection;

	    public async void Run(IBackgroundTaskInstance taskInstance)
	    {
		    _serviceDeferral = taskInstance.GetDeferral();

		    AppServiceTriggerDetails triggerDetail = taskInstance.TriggerDetails as AppServiceTriggerDetails;
		    if (triggerDetail?.Name == "TestVoiceCommandService")
		    {
			    try
			    {
				    _cortanaConnection = VoiceCommandServiceConnection.FromAppServiceTriggerDetails(triggerDetail);
				    _cortanaConnection.VoiceCommandCompleted += ServiceConnectionOnVoiceCommandCompleted;

				    VoiceCommand command = await _cortanaConnection.GetVoiceCommandAsync();
				    if (command.CommandName == "searchTrain")
				    {
					    await SendMessageForSearchTrain(command.Properties);
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

	    private async Task SendMessageForSearchTrain(IReadOnlyDictionary<string, IReadOnlyList<string>> input)
	    {
		    try
		    {
			    string date = input["date"].FirstOrDefault();
			    string origin = input["origin"].FirstOrDefault();
			    string destination = input["destination"].FirstOrDefault();

			    VoiceCommandUserMessage userMessage = new VoiceCommandUserMessage
			    {
				    DisplayMessage = $"Voyage de {origin} à {destination} {date}",
				    SpokenMessage = $"Voyage pour {destination} {date}"
			    };

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
					    ContentTileType = VoiceCommandContentTileType.TitleWithText,
					    AppLaunchArgument = $"date={tripDate.ToString("yyyy-MM-dd_HH:mm:ss")}&destination={destination}&origin={origin}",
					    Title = $"{origin} => {destination}",
					    TextLine1 = $"Le {tripDate.ToString("dd/MM/yyyy")} à {tripDate.ToString("HH:mm")}",
					    TextLine2 = $"2ème classe",
					    TextLine3 = $"Prix : 42€",
					    Image = await StorageFile.GetFileFromApplicationUriAsync(new Uri(
						    "ms-appx:///Assets/bouh_68x68.jpg"
						    //"ms-appx:///Assets/bouh_68x92.jpg"
						    //"ms-appx:///Assets/bouh_280x140.png"
						    ))
				    };

				    resultTiles.Add(tile);
			    }

			    VoiceCommandResponse response = VoiceCommandResponse.CreateResponse(userMessage, resultTiles);
			    response.AppLaunchArgument = $"date={originDate.ToString("yyyy-MM-dd_HH:mm:ss")}&destination={destination}&origin={origin}";

			    await _cortanaConnection.ReportProgressAsync(VoiceCommandResponse.CreateResponse(new VoiceCommandUserMessage()
			    {
				    SpokenMessage = "Recherche en cours",
				    DisplayMessage = "Recherche de train en cours"
			    }));

			    await Task.Delay(4000);

			    await _cortanaConnection.ReportSuccessAsync(response);
		    }
		    catch (Exception e)
		    {
			    Debug.WriteLine("Exception " + e);
		    }
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
	}
}
