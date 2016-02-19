using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Media.SpeechRecognition;
using Windows.Storage;

namespace BasicVoiceCommandActivation
{
	public static class Cortana
	{
		public static async Task InstallVCDAsync()
		{
			StorageFile storageFile = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///VoiceCommandDefinition.xml"));
			await Windows.ApplicationModel.VoiceCommands.VoiceCommandDefinitionManager.InstallCommandDefinitionsFromStorageFileAsync(storageFile);
		}

		public static void HandleLaunch(VoiceCommandActivatedEventArgs args)
		{
			List<string> rulePathList = args.Result.RulePath.ToList();
			IReadOnlyDictionary<string, IReadOnlyList<string>> semantic = args.Result.SemanticInterpretation.Properties;

			Dictionary<string, List<string>> semanticResult = semantic.ToDictionary(item => item.Key, item => item.Value.ToList());

			Debug.WriteLine(args.Result.Text);
		}
	}
}
