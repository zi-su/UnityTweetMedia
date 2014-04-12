using UnityEngine;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
using System.Security.Cryptography;

public class OAuth : MonoBehaviour {

	public string consumerKey;
	public string consumerSecret;
	const string REQ_TOKEN_URL   = "https://api.twitter.com/oauth/request_token?oauth_callback=oob";
	const string OAUTH_URL       = "http://api.twitter.com/oauth/authorize?oauth_token={0}";
	const string ACCESS_TOKEN_URL= "https://api.twitter.com/oauth/access_token";

	private string request_token;
	private string request_token_secret;

	private string access_token;
	private string access_token_secret;

	private string pincode;

	public string CONSUMER_KEY{
		get{return consumerKey;}
	}
	public string CONSUMER_SECRET{
		get{return consumerSecret;}
	}

	public string ACCESS_TOKEN{
		get{return access_token;}
	}
	public string ACCESS_TOKEN_SECRET{
		get{return access_token_secret;}
	}

	public string PINCODE{
		get{return pincode;}
		set{pincode = value;}
	}

	// Use this for initialization
	void Start () {
		pincode = "enter your pincode";
		LoadAccessToken();
		if(string.IsNullOrEmpty(access_token) && string.IsNullOrEmpty(access_token_secret)){
			RequestToken();
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void RequestToken(){
		StartCoroutine(CoRequestToken());
	}

	void LoadAccessToken(){
		access_token = PlayerPrefs.GetString("ACCESS_TOKEN");
		access_token_secret = PlayerPrefs.GetString("ACCESS_TOKEN_SECRET");
	}

	void ClearAccessToken(){
		PlayerPrefs.DeleteKey("ACCESS_TOKEN");
		PlayerPrefs.DeleteKey("ACCESS_TOKEN_SECRET");
	}

	void TestTweet(){
		Texture2D tex = new Texture2D(10,10, TextureFormat. ARGB32, false);
		for(int i = 0 ; i < tex.width ; i++){
			for(int j = 0 ; j < tex.height ; j++){
				tex.SetPixel(i, j, Color.black);
			}
		}
		tex.Apply();
		GetComponent<Twitter>().PostTweetMedia("test" , tex.EncodeToPNG());
	}

	IEnumerator CoRequestToken(){
		Hashtable hash = new Hashtable();
		hash["Authorization"] = MakeRequsetTokenHeader();
		Debug.Log(hash["Authorization"]);
		byte[] dummy = new byte[1];
		dummy[0] = 0;
		WWW www = new WWW(REQ_TOKEN_URL, dummy, hash);
		yield return www;
		if(string.IsNullOrEmpty(www.error)){
			Debug.Log(www.text);
			request_token = Regex.Match(www.text, @"oauth_token=([^&]+)").Groups[1].Value;
			request_token_secret = Regex.Match(www.text, @"oauth_token_secret=([^&]+)").Groups[1].Value;

			Application.OpenURL(string.Format(OAUTH_URL, request_token));
		}
		else{
			Debug.Log(www.error);
		}
	}

	string MakeRequsetTokenHeader(){
		string header = "";
		string nonce = GenerateNonce();
		string timestamp = GenerateTimeStamp();

		string signature = GetSignature("POST",REQ_TOKEN_URL, nonce, timestamp);
		header = "OAuth "; 
		header += "oauth_consumer_key=\"" + consumerKey + "\",";
		header += "oauth_nonce=\""+nonce + "\",";
		header += "oauth_signature=\""+signature + "\",";
		header += "oauth_signature_method=\"HMAC-SHA1\",";
		header += "oauth_timestamp=\"" + timestamp + "\",";
		header += "oauth_version=\"1.0\"";

		return header;
	}

	string GetSignature(string type, string url, string nonce, string timestamp){
		string signature = "";
		signature = type + "&";
		signature += WWW.EscapeURL(NormalizeURL(url)) + "&";

		signature += WWW.EscapeURL("oauth_callback=oob&oauth_consumer_key=" + consumerKey
		                           + "&oauth_nonce=" + nonce
								   + "&oauth_signature_method=HMAC-SHA1&" 
		                           + "oauth_timestamp="+timestamp+"&oauth_version=1.0");

		signature = Regex.Replace(signature, "(%[0-9a-f][0-9a-f])", s => s.Value.ToUpper());
		string key = string.Format("{0}&{1}",
		                           WWW.EscapeURL(consumerSecret),string.Empty);
		HMACSHA1 hmacsha1 = new HMACSHA1(System.Text.Encoding.UTF8.GetBytes(key));

		string str_signature = Convert.ToBase64String(
			hmacsha1.ComputeHash(
			Encoding.UTF8.GetBytes( signature )
			)
			);
		str_signature = WWW.EscapeURL(str_signature, Encoding.UTF8);
		return str_signature;
	}

	public void RequestAccessToken(){
		StartCoroutine(CoRequestAccessToken());
	}

	IEnumerator CoRequestAccessToken(){
		Hashtable header = new Hashtable();
		header["Authorization"] = MakeAccessTokenHeader();
		byte[] dummy = new byte[1];
		dummy[0] = 1;
		WWW www = new WWW(ACCESS_TOKEN_URL, dummy, header);
		yield return www;
		if(string.IsNullOrEmpty(www.error)){
			Debug.Log(www.text);
			access_token = Regex.Match(www.text, @"oauth_token=([^&]+)").Groups[1].Value;
			access_token_secret = Regex.Match(www.text, @"oauth_token_secret=([^&]+)").Groups[1].Value;

			PlayerPrefs.SetString("ACCESS_TOKEN", access_token);
			PlayerPrefs.SetString("ACCESS_TOKEN_SECRET", access_token_secret);
			Debug.Log(access_token);
			Debug.Log(access_token_secret);
		}
		else{
			Debug.Log(www.error);
		}
	}

	string MakeAccessTokenHeader(){
		string header;
		header = "";
		string nonce = GenerateNonce();
		string timestamp = GenerateTimeStamp();
		string signature = GetSignature("POST",ACCESS_TOKEN_URL, nonce, timestamp);
		header = "OAuth "; 
		header += "oauth_consumer_key=\"" + consumerKey + "\",";
		header += "oauth_nonce=\""+nonce + "\",";
		header += "oauth_signature=\""+signature + "\",";
		header += "oauth_signature_method=\"HMAC-SHA1\",";
		header += "oauth_timestamp=\"" + timestamp + "\",";
		header += "oauth_token=\""+  request_token + "\",";
		header += "oauth_verifier=\"" + pincode + "\",";
		header += "oauth_version=\"1.0\"";

		return header;
	}
	string NormalizeURL(string url){
		string normalizeURL;
		Uri uri = new Uri(url);
		normalizeURL = string.Format("{0}://{1}", uri.Scheme, uri.Host);
		normalizeURL += uri.AbsolutePath;

		return normalizeURL;
	}
	public string GenerateNonce(){
		return new System.Random().Next(123400, int.MaxValue).ToString();
	}
	
	public string GenerateTimeStamp(){
		TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);		
		return Convert.ToInt64(
			ts.TotalSeconds,
			CultureInfo.CurrentCulture
			)
			.ToString( CultureInfo.CurrentCulture );
	}
}
