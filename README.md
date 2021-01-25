# PwdCrypter (Open Source version)
PwdCrypter App multiplatform (Xamarin).  
IDE: VisualStudio 2019.

# TODO
- Translate all comments into English;
- Create a configuration file where put all the values that are now a constant into the code (and don't push that file here, please).

# How to customize your PwdCrypter App
Here the instructions for customize your PwdCrypter App and tools

## PwdCrypterNativeMessagingHost
You have to set the references to you in the setup of the PwdCrypter Native Messaging Host application.  
This application is used for the browser AddOn for Firefox, Chrome and Edge.  
Open the setup.iss file and edit the following defines:

<pre>
#define MyAppVersion "<YOUR_VERSION>"
#define MyAppPublisher "<YOUR_NAME>"
#define MyAppURL "<YOUR_WEBSITE>"
</pre>

About the registry, replace the place holder of the last four lines with your package name (for example: <YOUR_PACKAGE_NAME> = com.your_name.pwdcrypter).

Update the license files license-English.txt and license-Italian.txt:
- replace <LAST_UPDATE_DATE> with the last update date of your license
- replace <YOUR_NAME> with your name
- replace <YOUR_EMAIL> with your contact email
- replace <YOUR_WEBSITE> with your website's url to get support (if you have one). You are free to remove that line if you don't have any support module

Update the manifest files mmh_manifest_firefox.json and mmh_manifest.json:
- replace <YOUR_PACKAGE_NAME> with your one (for example com.your_name.pwdcrypter)
- replace <YOUR_EXTENSION> with your name (for example pwdcrypter@your_name.com or chrome-extension://my-chrome-code-extension/)

Open the NativeMessagingHost.cs source file and set the constant PACKAGE_FAMILY_NAME with the correct name (for example: 00000company.PwdCrypter_aabbccddee000).

## PwdCrypter App
In the folder PwdCrypter there are the projects of the solution of the App. It is use the Xamarin technology in order to build a multiplatform App.  
Go to the shared project in the folder PwdCrypter and edit the privacy policy files in the folder PrivacyPolicy:
- replace <LAST_UPDATE_DATE> with the last update date of your privacy policy
- replace <YOUR_WEBSITE> with your website where the user can find the manual and get support
- replace <YOUR_EMAIL> with your email for contact
- rememeber to update the license as you need :blush:!

In the specific platform project for Android and UWP will be copied the same privacy policy files (PwdCrypter.Android/Assets/PrivacyPolicy and PwdCrypter.UWP/Assets/PrivacyPolicy).  
Make sure that the command script in the compiler event of the project it works as well.

In the string table search and replace the value of:
- YOUR_NAME with your name (for example 3DCrypter)
- YOUR_WEBSITE with the url of your website (for example https://3dcrypter.it)
- YOUR_WEBSITE_EN with the url of your website in English language (for example https://3dcrypter.it/lingua=1). Use the same url if you don't need that
- YOUR_WEBSITE_IT with the url of your website in Italian language (for example https://3dcrypter.it/lingua=0). Use the same url if you don't need that
- YOUR_MANUAL_URL_EN with the url where it is available the App's manual in English language
- YOUR_MANUAL_URL_IT with the url where it is available the App's manual in Italian language

Clearly, you can add all the translation you want!

Open the source file NewsListPage.xaml.cs and replace the value of the constant URL_WEBSITE_API_NEWS with the url of the API at your website to get the list of news.  
You can ignore that if you don't want to provide any news feature in App. Remember to hide that feature from the hamburger menu.

For some internal use files, the App needs to encrypt them using a password that depends on the device.  
Go to the source file App.xaml.cs of the common platform project and set your salt (SaltDevicePwd constant) and the device id (FakeDeviceId costant) to use when the App is not able to get any id of the used device.  
Do the same thing in the source file PushNotificationManager.cs of the specific platform project for UWP.

### Android platform
Update the Android manifest file in the folder PwdCrypter.Android/Properties/AndroidManifest.xml.  
Replace the YOUR_PACKAGE_NAME with your one (for example com.x3dcrypter.pwdcypter).  
Remember to update the manifest as your need if you want to remove or add new features.

### Windows 10 platform (UWP)
Update the manifest of the Chrome AddOn (installable also in Edge) in the folder PwdCrypter.UWP/ChromeExtension/manifest.json:
- replace YOUR_VERSION with the version of your AddOn
- replace YOUR_NAME wiht your name

Do the same thing to the Firefox AddOn in the folder PwdCrypter.UWP/FirefoxExtension/manifest.json:
- replace YOUR_VERSION with the version of your AddOn
- replace <YOUR_EXTENSION> with your one (for example pwdcrypter@your_name.com)

Check the declaration in the Package.appxmanifest and create your certificate using the VisualStudio tool.

### OneSignal
The App is able to show push notification by the OneSignal service. If you don't know what it is, get a look here <A HREF="https://onesignal.com/">https://onesignal.com/</A>.

For Android, you have to manually open a notification channel.  
Go to the main activity source file MainActivity.cs of the specific platform project for Android and set your channel ID using the constant NotificationChannelID. Jump over the keyboard!

Open the source file AppSettings.cs of the common platform project and replace the constant key1, key2, key3, id1, id2 and id3 in the class OneSignalSettings with the one that you have to use in order to get your API Id.  
Why is it so complicated? This is a trick to protect a key in the code using the property of XOR. You can see <A HREF="https://stackoverflow.com/questions/11671865/how-to-protect-google-play-public-key-when-doing-inapp-billing">here</A> an example.

### InApp products
If you want to provide some additional component (InApp products) in your App, you have to use the BillingManager class.  
Go to the source file BillingManager.cs of the common platform project and set a value to the DevPayload constant. Here you can find the example of how it is handled the PLUS product through the two platforms GooglePlay and Windows Store.

Also for the billing verification for GooglePlay you should check the signature (it isn't mandatory).  
Open the source file BillingVerify.cs in the specific platform project for Android and replace the keys to get your GooglePlay public key.

The class CacheManager is the cache manager used to store the data about the products that the user have purchased. This class can reduce the amount of verifications in order to check if a specific product was purchased.  
Open the source code CacheManager.cs of the common platform project and replace the constant CachePassword1, CachePassword2, CachePassword3, CacheId1, CacheId2 and CacheId3 to get the encryption password of the cache file.

### Cloud
The App can save the list of password (and also the do backups) in the Cloud.
The class GoogleDriveConnector and OneDriveConnector provide the feature to access to the Cloud of GoogleDrive and OneDrive.  
Go to the source file Cloud/GoogleDriveConnector.cs of the common platform project and set:
- your application id to the constant IDApplication
- your callback URL to the constant URLCallaback
- your serialization password for the remember me feature to the constant SerializationPassword

Do the same thing in the source file Cloud/OneDriveConnector.cs:
- set your application id to the constant IDApplication
- set your app secret to the constant AppSecret
- set your serialization password to the constant SerializationPassword
