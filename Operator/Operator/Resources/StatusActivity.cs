﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Text;
using System.Threading;
using Android.Gms.Maps;
using Android.Gms.Common.Apis;
using Android.Locations;

namespace Operator.Resources
{
    [Activity(Label = "StatusActivity")]
    public class StatusActivity : Activity, Android.Gms.Location.ILocationListener
    {
        TextView statusText;
        TextView detailsText;

        Location location;
        Timer refreshTimer;
        string emergencyId;
        bool trackLocation;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.StatusLayout);
            statusText = FindViewById<TextView>(Resource.Id.statusText);
            detailsText = FindViewById<TextView>(Resource.Id.detailsText);

            emergencyId = Intent.GetStringExtra("Id");
            trackLocation = Intent.GetBooleanExtra("TrackLocation", false);

            refreshTimer = new Timer(new TimerCallback((obj) => { UpdateTracking(); }), null, 0, 3000);
        }

        private void UpdateTracking()
        {
            ActiveEmergency emergency = ServerHelper.GetActiveEmergency(emergencyId);

            setStatusDisplay(emergency.status);
            detailsText.Text = emergency.response;
            
            // Stop updates and switch to EndActivity if status is complete, archived or trash
            if (emergency.status > 2)
            {
                Intent intent = new Intent(this, typeof(EndActivity));
                intent.PutExtra("FinalStatus", emergency.status);

                GetSharedPreferences("prefs", FileCreationMode.Private).Edit().Clear().Commit();

                StartActivity(intent);

                refreshTimer.Dispose();
                Finish();
            }

            // Update display
            RunOnUiThread(new Action(() =>
            {
                setStatusDisplay(emergency.status);
                detailsText.Text = emergency.response;
            }));

            // Update and transmit location
            if (trackLocation && location != null)
            {
                GeocodedLocation geocodedLocation = new GeocodedLocation();
                geocodedLocation.latitude = (float)location.Latitude;
                geocodedLocation.longitude = (float)location.Longitude;

                ServerHelper.SubmitLocation(geocodedLocation, emergencyId, this);
            }
        }

        private void setStatusDisplay(int status)
        {
            switch (status)
            {
                case 1:
                    statusText.SetText(Html.FromHtml("<font color='#F24333'>⬤</font> Pending"), TextView.BufferType.Spannable);
                    break;
                case 2:
                    statusText.SetText(Html.FromHtml("<font color='#FBFE4F'>⬤</font> In Progress"), TextView.BufferType.Spannable);
                    break;
                case 3:
                    statusText.SetText(Html.FromHtml("<font color='#4ACE82'>⬤</font> Complete"), TextView.BufferType.Spannable);
                    break;
                case 4:
                    statusText.SetText(Html.FromHtml("<font color='#78C0E0'>⬤</font> Archived"), TextView.BufferType.Spannable);
                    break;
                case 5:
                    statusText.SetText(Html.FromHtml("<font color='#8796BA'>⬤</font> Trash"), TextView.BufferType.Spannable);
                    break;
            }
        }
        
        public void OnLocationChanged(Location location)
        {
            this.location = location;
        }
    }
}