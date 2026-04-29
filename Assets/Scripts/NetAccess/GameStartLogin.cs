using Networking;
using UnityEngine;

namespace NetAccess
{
    public class GameStartLogin : MonoBehaviour
    {
        public GameObject loginModal;
        public string username;
        public string password;

        private void Start()
        {
            loginModal.SetActive(false);
        }

        public void OpenModal()
        {
            loginModal.SetActive(true);
        }
        
        public void SendLogin()
        {
            API.Login(username, password).OnResponse(res =>
            {
                Networking.Networking.SaveAccessToken(res.token);
                loginModal.SetActive(false);
            }).Build();
        }
        
        public void SendSignup()
        {
            API.Signup(username, password).OnResponse(res =>
            {
                Networking.Networking.SaveAccessToken(res.token);
                loginModal.SetActive(false);
            }).Build();
        }

        public void UpdateUsername(string uName)
        {
            username = uName;
        }

        public void UpdatePassword(string pwd)
        {
            password = pwd;
        }
    }
}
