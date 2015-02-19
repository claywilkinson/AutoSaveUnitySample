/*
 * Copyright (C) 2015 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using UnityEngine;
using System.Collections;
using GooglePlayGames.BasicApi;
using GooglePlayGames;
using UnityEngine.UI;
using GooglePlayGames.BasicApi.SavedGame;
using System;

/**
 * This class demonstrates how to perform autosave using the Google Play Game Services.
 * 
 * This script should be attached to the Canvas GameObject in the Scene.
 * 
 */
using System.IO;


public class AutoSave : MonoBehaviour {

	// Public variables that should be assigned via the inspector.

	// status message to see what is happening.
	public GameObject mStatus;

	// floating point input field
	public InputField mFloatText;

	// string input
	public InputField mStringText;

	// int32 input
	public InputField mIntText;

	// the sign-in/sign-out button.
	public Button mSignInButton;

	// The filename for the autosaved game data.
	// It can be any valid filename.
	private string autoSaveFileName = "AutoSave";

	// time since last auto save
	private float mTimeSinceLastSave;
	private bool mModified;

	// the interval for auto saving
	private float mAutoSaveInterval = 30f;

	// these are variables persisted in the saved game data.
	private float mSavedValue = 0;
	private string mSavedMessage = "";
	private int mSavedCounter = 0;

	// these are fields that are part of the metadata saved with the game data.
	private TimeSpan mTotalPlayingTime;
	private Texture2D mScreenImage;


	// internal message string, displayed in mStatus
	string mMsg ="";

	// keep track of out playing time.
	DateTime mStartTime;

	// callback for logging in.
	System.Action<bool> mAuthCallback;

	// Use this for initialization
	void Start () {

		// start with unmodified data.
		mModified = false;

		//This is the callback that is called when signing in.
		mAuthCallback = (bool success) => {

			// When successful - change the text of the sign-in button
			// and load the auto saved file.
			if (success) {
				Debug.Log("Authentication was successful!");
				mSignInButton.GetComponentInChildren<Text>().text = "Sign Out";
				mMsg = "";
				LoadAutoSave();
			}
			else {
				Debug.LogWarning("Authentication failed!");
				mSignInButton.GetComponentInChildren<Text>().text = "Sign In";
			}
			
		};
		
		// Set the configuration enabling SavedGames API.
		PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder()
			.EnableSavedGames()
			.Build();
		PlayGamesPlatform.InitializeInstance(config);
		
		// Activate the Play Games platform. This will make it the default
		// implementation of Social.Active
		PlayGamesPlatform.Activate();
		
		// enable debug logs (note: we do this because this is a sample; on your production
		// app, you probably don't want this turned on by default, as it will fill the user's
		// logs with debug info).
		PlayGamesPlatform.DebugLogEnabled = true;
		
		//Login explicitly for this sample, usually this would be silent
		PlayGamesPlatform.Instance.Authenticate(mAuthCallback, false);


		// Initialize the input fields with validation rules.
		mFloatText.characterValidation = InputField.CharacterValidation.Decimal;
		mStringText.characterValidation = InputField.CharacterValidation.None;
		mIntText.characterValidation = InputField.CharacterValidation.Integer;

		// Set the values of the input fields to their initial values.
		UpdateDisplay();
	}
	
	// Update is called once per frame
	void Update () {

		mTimeSinceLastSave += Time.deltaTime;

		// Update the status text
		string status;
		if (Social.localUser.authenticated) {
			status = "Authenticated ";
			// need to be authenticated to do auto save.
			if (mTimeSinceLastSave > mAutoSaveInterval) {
				// only save if modified.
				if (mModified) {
					AutoSaveData();
				}
				mTimeSinceLastSave = 0f;
      		}
		}
		else {
			status = "Not Authenticated";
		}
		mStatus.GetComponent<Text>().text = status + " " + mMsg;


	}

	// This is a helper method used to create an image that is part of the 
	// saved game data metadata.  It can be used to give a visual indication of what the 
	// saved game state is.
	public void CaptureScreenshot() {
		mScreenImage = new Texture2D(Screen.width, Screen.height);
		mScreenImage.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
		mScreenImage.Apply();
	}

	// Called to Save the data - linked to the OnClick event for the Save button in the UI.
	public void OnSave() {
		SaveData();
		UpdateDisplay();
	}

	public void OnLoad() {
		LoadData();
		UpdateDisplay();
	}

	// Called to sign in or sign out.  This is linked to the Sign-in button OnClick event.
	// if the user is signed in, this signs the user out.  Conversely, if they are not signed in,
	// it signs them in.
	public void OnSignIn() {
		if (Social.localUser.authenticated) {
			PlayGamesPlatform.Instance.SignOut();
			mSignInButton.GetComponentInChildren<Text>().text = "Sign In";
		}
		else {
			PlayGamesPlatform.Instance.Authenticate(mAuthCallback, false);

		}
	}

	// Called to change the float value of our application.  It is linked to OnEndEdit event of the 
	// float input edit.
	public void OnFloatChanged(string value) {
		if (value.Length > 0){
			mSavedValue = System.Convert.ToSingle(value);
			mModified = true;
    	}
	}

	// Called to change the string value of our application.  It is linked to OnEndEdit event for the
	// string input edit.
	public void OnStringChanged(string value) {
		mSavedMessage = value;
		mModified = true;
	}

	// Called to change the int value of our application.  It is linked to the OnEndEdit event for the 
	// int input field.
	public void OnIntChanged(string value) {
		if (value.Length > 0) {
			mSavedCounter = System.Convert.ToUInt16(value);
			mModified = true;
    	}
	}

	// Update the edit fields.
	void UpdateDisplay() {
		
		mFloatText.text = mSavedValue.ToString();
		mStringText.text = mSavedMessage;
		mIntText.text = mSavedCounter.ToString();
	}

	void LoadData() {
		((PlayGamesPlatform)Social.Active).SavedGame.ShowSelectSavedGameUI("Select Saved Game to Load",
		                                                                   10,
		                                                                   false,
		                                                                   true,
		                                                                   SaveGameSelectedForRead);
  	}

	void SaveData() {
		((PlayGamesPlatform)Social.Active).SavedGame.ShowSelectSavedGameUI("Save Game Progress",
		                                                                   10,
		                                                                   true,
		                                                                   false,
		                                                                   SaveGameSelectedForWrite);
    
    
  }

	// Starts the save Data process.  Call this to start saving. 
	void AutoSaveData() {
		// grab the frame.
		CaptureScreenshot();
		//save to the autosave file name.
		((PlayGamesPlatform)Social.Active).SavedGame.OpenWithAutomaticConflictResolution(autoSaveFileName,
		                                                                                 DataSource.ReadCacheOrNetwork,
		                                                                                 ConflictResolutionStrategy.UseLongestPlaytime,
		                                                                                 SavedGameOpenedForWrite);


	}

	//  Starts the loading process for the saved game data.
	void LoadAutoSave() {
		//open the data.
		((PlayGamesPlatform)Social.Active).SavedGame.OpenWithAutomaticConflictResolution(autoSaveFileName,
		                                                                                 DataSource.ReadCacheOrNetwork,
		                                                                                 ConflictResolutionStrategy.UseLongestPlaytime,
		                                                                                 SavedGameOpenedForRead);

	}

	// Callback called by GPG when the file is opened.
	public void SavedGameOpenedForWrite(SavedGameRequestStatus status, ISavedGameMetadata game) {
		if(status == SavedGameRequestStatus.Success) {
			mMsg = "Opened, writing...";
			Debug.Log (mMsg);

			// this is the screen image for the metadata.
			byte[] pngData = (mScreenImage!=null) ?mScreenImage.EncodeToPNG():null;

			// Call WriteValuesToBytes to convert our fields that we need to save to a byte array.
			byte[] data = WriteValuesToBytes();

			// Update the total playing time.
			TimeSpan playedTime = mTotalPlayingTime.Add(DateTime.Now.Subtract(mStartTime));

			string description;

			if (game.Filename.Equals(autoSaveFileName)) {
				description = "Autosaved game at " + DateTime.Now;
			}
			else {
				description = "Saved Game at " + DateTime.Now;
			}

			// Build the metadata
			SavedGameMetadataUpdate.Builder builder =  new 
				SavedGameMetadataUpdate.Builder()
					.WithUpdatedPlayedTime(playedTime)
					.WithUpdatedDescription(description);
			
			if (pngData != null) {
				Debug.Log("Save image of len " + pngData.Length);
				builder = builder.WithUpdatedPngCoverImage(pngData);
			}
			else {
				Debug.Log ("No image avail");
			}
			SavedGameMetadataUpdate updatedMetadata  = builder.Build();

			// Finally commit the data and metadata to the saved game.
			((PlayGamesPlatform)Social.Active).SavedGame.CommitUpdate(game,updatedMetadata,data,SavedGameWritten);
		} else {
			mMsg = "Error opening game: " + status + " filename = " + game.Filename;
			Debug.LogWarning(mMsg);
		}
	}

	public void SaveGameSelectedForWrite(SelectUIStatus status, ISavedGameMetadata game) {
		if(status == SelectUIStatus.SavedGameSelected) {
			string newFilename;
			mMsg = "Selected Game to save";
			Debug.Log (mMsg);
			if (game.Filename == null || game.Filename.Length == 0) {

				newFilename = "UserSaved_" + DateTime.Now.ToBinary();
			}
			else {
				newFilename = game.Filename;
			}
			((PlayGamesPlatform)Social.Active).SavedGame.OpenWithAutomaticConflictResolution(newFilename,
			                                                                                 DataSource.ReadCacheOrNetwork,
			                                                                                 ConflictResolutionStrategy.UseLongestPlaytime,
			                                                                                 SavedGameOpenedForWrite);
		}
		else {
			mMsg = "Selection failed: " + status;
			Debug.LogWarning(mMsg);
    }
    
  }
  
  public void SaveGameSelectedForRead(SelectUIStatus status, ISavedGameMetadata game) {
		if(status == SelectUIStatus.SavedGameSelected) {
			mMsg = "Selected Game to load";
			Debug.Log (mMsg);
			((PlayGamesPlatform)Social.Active).SavedGame.OpenWithAutomaticConflictResolution(game.Filename,
			                                                                                 DataSource.ReadCacheOrNetwork,
			                                                                                 ConflictResolutionStrategy.UseLongestPlaytime,
			                                                                                 SavedGameOpenedForRead);
		}
		else {
			mMsg = "Selection failed: " + status;
			Debug.LogWarning(mMsg);
		}
      
	}

	// Callback called by GPG when the file is opened.
	public void SavedGameOpenedForRead(SavedGameRequestStatus status, ISavedGameMetadata game) {
		if(status == SavedGameRequestStatus.Success) {
			mMsg = "Opened, reading...";
			Debug.Log (mMsg);

			// Record the total playing time so we can add more time to it later.
			mTotalPlayingTime = game.TotalTimePlayed;
				((PlayGamesPlatform)Social.Active).SavedGame.ReadBinaryData(game,SavedGameLoaded);
		} else {
			mMsg = "Error opening game: " + status;
			Debug.LogWarning(mMsg);
		}
	}

	// Callback called by GPG when the bytes are written to the server.
	public void SavedGameWritten(SavedGameRequestStatus status, ISavedGameMetadata game) {
		if(status == SavedGameRequestStatus.Success) {
			mMsg = "Game " + game.Description + " written!";
			Debug.Log(mMsg);

		} else {
			mMsg = "Error saving game: " + status;
			Debug.LogWarning(mMsg);
		}
	}


	// Callback when the bytes are read from the server.
	public void SavedGameLoaded(SavedGameRequestStatus status, byte[] data) {
		if (status == SavedGameRequestStatus.Success) {
			mMsg = "SaveGameLoaded, success=" + status;
			Debug.Log(mMsg);

			// Convert the bytes back into the values we are saving.
			ReadValuesFromBytes(data);
		} else {
			mMsg = "Error reading game: " + status; 
			Debug.LogWarning(mMsg);
		}
	}

	// Good practice to version the serialized data so if we change it, we can be backwards compatible.
	const string VERSION_MARKER = "V1";

	// delimiter - expected not to be in the actual saved data.
	const char DELIMITER = '|';

	// Converts our values we want to save into a byte array.
	byte[] WriteValuesToBytes() {

		MemoryStream ms = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(ms);
		writer.Write(VERSION_MARKER);
		writer.Write(mSavedValue);
		writer.Write(mSavedMessage);
		writer.Write(mSavedCounter);
		writer.Flush();
		return ms.ToArray();
	}

	// Converts the byte array into our saved values.  This reading has to match the WriteValuesToBytes() 
	// implementation so the fields are read in the correct order.
	void ReadValuesFromBytes(byte[] data) {
		if(data.Length == 0) {
			// this is a new file - never written to
			mMsg = "New saved file, no data";

		}
		else {
			MemoryStream ms = new MemoryStream(data);
			BinaryReader reader = new BinaryReader(ms);
			string version = reader.ReadString();
			if (version.Equals(VERSION_MARKER)) {
				// this is version one serialization.
				mSavedValue = reader.ReadSingle();
				mSavedMessage = reader.ReadString();
				mSavedCounter = reader.ReadInt32();
				mMsg = "Loaded Saved Data";
			
			}
			else {
				mMsg = "Unknown serialization version: " + version;
				Debug.LogError(mMsg);
			}
		}
		UpdateDisplay();
		mStartTime = DateTime.Now;
	}

}
