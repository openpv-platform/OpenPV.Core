using Ahsoka.DeveloperTools.Spark;
using Ahsoka.DeveloperTools.ViewModel;
using Ahsoka.Utility;
using Avalonia.Threading;
using EnovationCloud.Core.Client;
using EnovationCloud.Core.DataTransferObjects;
using EnovationCloud.Core.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools;

internal class SparkLoginViewModel : ChildViewModelBase<MainViewModel>
{
    #region Fields
    readonly SparkSettings settings;
    private string loginName;
    private string companyName;
    private SecureString password;
    private bool rememberMe;
    private bool isLogginIn;
    private SecureString oldPassword;
    private SecureString newPassword;
    private SecureString newPasswordConfirmed;
    private string firstName;
    private string lastName;
    private string emailAddress;
    private bool isAdvanced = false;
    private readonly List<string> sparkList = new();
    #endregion

    #region Properties

    public bool IsAdvanced
    {
        get { return isAdvanced; }
    }

    public string FirstName
    {
        get { return firstName; }
        set { firstName = value; OnPropertyChanged(); }
    }

    public string LastName
    {
        get { return lastName; }
        set { lastName = value; OnPropertyChanged(); }
    }

    public string EmailAddress
    {
        get { return emailAddress; }
        set { emailAddress = value; OnPropertyChanged(); }
    }

    public string CompanyName
    {
        get { return companyName; }
        set { companyName = value; OnPropertyChanged(); }
    }

    public string LoginName
    {
        get { return loginName; }
        set { loginName = value; OnPropertyChanged(); }
    }

    public string Password
    {
        get { return SecureLocalStrings.ToInsecureString(password); }
        set { password = SecureLocalStrings.ToSecureString(value); ; OnPropertyChanged(); }
    }

    public bool IsLoggingIn
    {
        get { return isLogginIn; }
        set { isLogginIn = value; OnPropertyChanged(); }
    }

    public bool RememberMe
    {
        get { return rememberMe; }
        set { rememberMe = value; OnPropertyChanged(); }
    }

    public string SparkUrl
    {
        get { return settings.Url; }
        set { settings.Url = value; OnPropertyChanged(); }
    }

    public List<string> SparkUrlList
    {
        get { return sparkList; }
    }

    public string PasswordOld
    {
        get { return oldPassword != null ? SecureLocalStrings.ToInsecureString(oldPassword) : string.Empty; }
        set { oldPassword = SecureLocalStrings.ToSecureString(value); ; OnPropertyChanged(); }
    }

    public string PasswordNew
    {
        get { return newPassword != null ?  SecureLocalStrings.ToInsecureString(newPassword) : string.Empty; }
        set { newPassword = SecureLocalStrings.ToSecureString(value); ; OnPropertyChanged(); }
    }

    public string PasswordNewConfirmed
    {
        get { return newPasswordConfirmed != null ? SecureLocalStrings.ToInsecureString(newPasswordConfirmed) : string.Empty; }
        set { newPasswordConfirmed = SecureLocalStrings.ToSecureString(value); OnPropertyChanged(); }
    }
    #endregion

    #region Methods
    public SparkLoginViewModel(MainViewModel parentViewModel) :
        base(parentViewModel)
    {
        settings = LoadSettings();

        // This will be called 
        SetReleaseValues();

        // if Debug, let the user pick.
        SetDebugValues();

        if (settings.RememberMe && !String.IsNullOrEmpty(settings.RefreshToken))
        {
           
            RememberMe = true;
            LoginName = settings.LoginName;

            AuthorizationDTO authDTO = new()
            {
                Refresh = SecureLocalStrings.ToInsecureString(StringEncryption.DecryptString(settings.RefreshToken))
            };

            if (ParentViewModel.IsOffline)
            {
                var result = SparkAccountClient.SharedSparkClient.IsRefreshTokenStillValid(authDTO);
                if (result)
                {
                    // Allow Login to Continue without Logging In.
                    parentViewModel.LogIn();
                    return;
                }    
            };

            try
            {
                authDTO = SparkAccountClient.SharedSparkClient.LoginWithRefreshToken(settings.Url, authDTO, LogoutCompleted);
                settings.RefreshToken = StringEncryption.EncryptString(SecureLocalStrings.ToSecureString(authDTO.Refresh));

                SaveSettings(settings);

                var account = SparkAccountClient.SharedSparkClient.GetAccount();
                LastName = account.LastName;
                FirstName = account.FirstName;
                EmailAddress = account.Email;
                parentViewModel.LogIn();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                settings.RefreshToken = string.Empty;
                SaveSettings(settings);
            }
        }
    }

    [Conditional("RELEASE")]
    private void SetReleaseValues()
    {
        // Set Default URL
        sparkList.Add("enovation.powervisionspark.com");
        SparkUrl = SparkUrlList.FirstOrDefault();
    }

    [Conditional("DEBUG")]
    private void SetDebugValues()
    {
        sparkList.Add("enovation.powervisionspark.com");
        sparkList.Add("development.powervisionspark.com");
        sparkList.Add("sparkserviceprod-workspace.azurewebsites.net");
        sparkList.Add("localhost:7211");
        SparkUrl ??= SparkUrlList.FirstOrDefault();

        isAdvanced = true;
    }

    SparkSettings LoadSettings()
    {
        try
        {
            if (File.Exists(ConnectionFilePath()))
                return JsonUtility.Deserialize<SparkSettings>(File.ReadAllText(ConnectionFilePath()));
            else
                return new SparkSettings();
        }
        catch (Exception) { return new SparkSettings(); }

    }

    private void SaveSettings(SparkSettings settings)
    {
        string data = JsonUtility.Serialize(settings);
        File.WriteAllText(ConnectionFilePath(), data);
    }

    private string ConnectionFilePath()
    {
        return CustomerToolPaths.GetSettingsPath("SparkInfo.json");
    }

    internal void LoginToSpark()
    {
        Task.Run(() =>
        {
            IsLoggingIn = true;
            return AttemptLoginInternal();

        }).ContinueWith(result =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (result.Result == "")
                {
                    ParentViewModel.LogIn();
                }
                else
                    ParentViewModel.Dialog.ShowDialog("Login Error", $"Unable to Login to Spark.  \r\n\r\nPlease check your 'User Name' and 'Password'.  Your login information should match your Spark 'User Name' and 'Password'.", "Ok");

                IsLoggingIn = false;
            });
        });
    }

    internal string AttemptLoginInternal()
    {
        try
        {
            var authToken = SparkAccountClient.SharedSparkClient.LoginToSpark(settings.Url, SparkEndPointType.Teams, SecurityGroopType.Account, LoginName, password, LogoutCompleted);
            settings.RememberMe = rememberMe;
            if (rememberMe)
            {
                settings.LoginName = loginName;
                settings.RefreshToken = StringEncryption.EncryptString(SecureLocalStrings.ToSecureString(authToken.Refresh));
            }
            else
            {
                settings.LoginName = string.Empty;
                settings.RefreshToken = string.Empty;
            }

            SaveSettings(settings);

            var account = SparkAccountClient.SharedSparkClient.GetAccount();
            LastName = account.LastName;
            FirstName = account.FirstName;
            EmailAddress = account.Email;

            return "";
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return ex.Message;
        }
    }

    internal void ChangePassword()
    {
        if (string.IsNullOrEmpty(SecureLocalStrings.ToInsecureString(oldPassword)) || string.IsNullOrEmpty(SecureLocalStrings.ToInsecureString(newPassword)) || string.IsNullOrEmpty(SecureLocalStrings.ToInsecureString(newPasswordConfirmed)))
        {
            ParentViewModel.Dialog.ShowDialog("Change Password", "Password must not be empty.", "Ok");
            return;
        }
        if (!SecureLocalStrings.ToInsecureString(newPassword).Equals(SecureLocalStrings.ToInsecureString(newPasswordConfirmed)))
        {
            ParentViewModel.Dialog.ShowDialog("Change Password", "New passwords do not match.", "Ok");
            return;
        }

        Task.Run(() =>
        {
            try
            {
                // Set Password
                password = oldPassword;
                // Login Again to Verify Password is Correct. 
                var returnValue = AttemptLoginInternal();

                if (returnValue.Equals(""))
                {
                    var account = SparkAccountClient.SharedSparkClient.GetAccount();
                    account.NewPassword = SecureLocalStrings.ToInsecureString(newPassword);
                    account.UpdatePassword = true;

                    // Update Password
                    SparkAccountClient.SharedSparkClient.UpdateAccount(account);
                    // Set to New Password
                    password = newPassword;
                    // Login Again to Verify Password is Correct. 
                    return AttemptLoginInternal();
                }
                return "Current Password Not Correct";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }).ContinueWith(result =>
        {
            string dialog;
            if (result.Result == "")
            {
                dialog = $"Password for Spark Account {LoginName} was changed successfuly";
                PasswordOld = string.Empty;
                PasswordNew = string.Empty;
                PasswordNewConfirmed = string.Empty;
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ParentViewModel.LogIn();
                });
            }
            else
                dialog = $"Password change failed with error: {result.Result}";

            ParentViewModel.Dialog.ShowDialog("Change Password", dialog, "Ok");
        });
    }

    internal void ForgotPassword()
    {
        Task.Run(() =>
        {
            try
            {
                SparkAccountClient.SharedSparkClient.RequestPasswordReset(settings.Url, LoginName);
                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }).ContinueWith(result =>
        {
            string dialog;
            if (result.Result == "")
            {
                dialog = $"Reset password message sent for Spark Account {LoginName}";
            }
            else
                dialog = $"Reset failed with error: {result.Result}";

            ParentViewModel.Dialog.ShowDialog("Forget Password", dialog, "Ok");
        });
    }

    internal void SignUp()
    {
        ParentViewModel.Popup.DismissPopup();
        ParentViewModel.Popup.ShowPopup(new Views.SparkSignupView() { DataContext = ParentViewModel.SparkLogin });
    }

    internal void CompleteSignUp()
    {
        IsLoggingIn = true;

        Task.Run(string () =>
        {
            try
            {
                if (string.IsNullOrEmpty(LoginName))
                {
                    ParentViewModel.Dialog.ShowDialog("Signup Error", "Please enter your email Aaddress", "Ok");
                    return "Signup Error";
                }

                if (string.IsNullOrEmpty(FirstName) || string.IsNullOrEmpty(LastName) || string.IsNullOrEmpty(CompanyName))
                {
                    ParentViewModel.Dialog.ShowDialog("Signup Error", "Please enter your First, Last and Company Name", "Ok");
                    return "Signup Error";
                }


                SparkAccountDTO dto = new()
                {
                    FirstName = FirstName,
                    LastName = LastName,
                    Email = LoginName,
                    Login = LoginName,
                    CompanyName = CompanyName,
                };

                return SparkAccountClient.SharedSparkClient.RequestNewAccount(SparkUrl, dto);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

        }).ContinueWith(result =>
        {
            IsLoggingIn = false;

            if (result.Result.Contains("New account request sent successfully"))
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ParentViewModel.Popup.DismissPopup();
                    ParentViewModel.Dialog.ShowDialog("Signup Complete", $"In the next few minutes, you should receive an email to confirm your email address. Once this verification is complete, return here to login to OpenPV.", "Ok");
                    ParentViewModel.Popup.ShowPopup(new Views.SparkLoginView() { DataContext = ParentViewModel.SparkLogin });
                });
            else if (result.Result.Contains("Account already exists"))
                ParentViewModel.Dialog.ShowDialog("Signup Error", $"{result.Result}", "Ok");
            else
                ParentViewModel.Dialog.ShowDialog("Signup Error", $"An error occured trying to request a new account.  Please verify you have internet access and try again. \r\nError:{result.Result}", "Ok");
        });
    }

    internal void BackToLogin()
    {
        ParentViewModel.Popup.DismissPopup();
        ParentViewModel.Popup.ShowPopup(new Views.SparkLoginView() { DataContext = ParentViewModel.SparkLogin });
    }

    internal void ChangeInfo()
    {
        Task.Run(() =>
        {
            try
            {
                string tempLastName = LastName;
                string tempFirstName = FirstName;
                string tempEmailAddress = EmailAddress;

                // Login Again to Verify Password is Correct. 
                var authToken = SparkAccountClient.SharedSparkClient.LoginWithRefreshToken(settings.Url, SparkAccountClient.SharedSparkClient.CurrentAuthorization, LogoutCompleted);

                if (authToken != null && authToken.Token != null)
                {
                    var account = SparkAccountClient.SharedSparkClient.GetAccount();

                    bool emailAddressChanged = account.Email != emailAddress;

                    account.LastName = tempLastName;
                    account.FirstName = tempFirstName;
                    account.Email = tempEmailAddress;

                    // Update Info
                    SparkAccountClient.SharedSparkClient.UpdateAccount(account);

                    return emailAddressChanged ? "Success: You should receive an email shortly to confirm your email address change." : "Success: Your information has been updated.";
                }

                return "Error: An error occurred updating your information.";
            }
            catch
            {
                return "Error: An error occurred updating your information.";
            }
        }).ContinueWith(result =>
        {
            if (result.IsFaulted)
                ParentViewModel.Dialog.ShowDialog("Update Error", result.Result, "Ok");
        });
    }

    internal void Cancel()
    {
        Environment.Exit(0);
    }

    internal void Logout()
    {
        SparkAccountClient.SharedSparkClient.RequestLogout();
    }

    internal void LogoutCompleted(WebException ex)
    {
        settings.RememberMe = false;
        settings.LoginName = string.Empty;
        settings.RefreshToken = string.Empty;

        SaveSettings(settings);

        Password = string.Empty;
        LoginName = string.Empty;

        ParentViewModel.Logout();
    }

    internal void ClosePopup()
    {
        ParentViewModel.OpenOptions();
    }
    #endregion
}

public class SparkSettings
{
    public bool RememberMe { get; set; }
    public string LoginName { get; set; }
    public string RefreshToken { get; set; }
    public string Url { get; set; }
}

