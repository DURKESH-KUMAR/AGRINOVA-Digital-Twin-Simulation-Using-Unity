using UnityEngine;
using Unity.Notifications.Android;
using System;

public class AgriNotificationManager : MonoBehaviour
{
    private string channelId = "agri_alerts";

    void Awake()
    {
        RegisterChannel();
    }

    void RegisterChannel()
    {
        var channel = new AndroidNotificationChannel()
        {
            Id = channelId,
            Name = "AgriNova Alerts",
            Importance = Importance.High,
            Description = "Onion storage alerts",
        };

        AndroidNotificationCenter.RegisterNotificationChannel(channel);
    }

    public void SendNotification(string status, string message)
    {
        var notification = new AndroidNotification();

        status = status.ToUpper();

        if (status == "OK")
        {
            notification.Title = "🧅 Storage Stable";
            notification.Text = message;
        }
        else if (status == "CAUTION")
        {
            notification.Title = "⚠ Storage Alert";
            notification.Text = message;
        }
        else if (status == "WARNING")
        {
            notification.Title = "🚨 Critical Warning!";
            notification.Text = message;
        }
        else
        {
            notification.Title = "Agri Update";
            notification.Text = message;
        }

        notification.FireTime = DateTime.Now.AddSeconds(10);
        notification.ShouldAutoCancel = true;

        AndroidNotificationCenter.SendNotification(notification, channelId);
    }
}