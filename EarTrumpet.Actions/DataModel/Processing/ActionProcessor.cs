﻿using EarTrumpet.DataModel;
using EarTrumpet.Extensibility;
using EarTrumpet.Extensibility.Shared;
using EarTrumpet_Actions.DataModel.Actions;
using System;
using System.Linq;

namespace EarTrumpet_Actions.DataModel.Processing
{
    class ActionProcessor
    {
        public static void Invoke(BaseAction a)
        {
            var playbackMgr = DataModelFactory.CreateAudioDeviceManager(AudioDeviceKind.Playback);

            if (a is SetVariableAction)
            {
                Addon.Current.LocalVariables[((SetVariableAction)a).Text] = ((SetVariableAction)a).Value;
            }
            else if (a is SetDefaultDeviceAction)
            {
                var dev = playbackMgr.Devices.FirstOrDefault(d => d.Id == ((SetDefaultDeviceAction)a).Device.Id);
                if (dev != null)
                {
                    playbackMgr.SetDefaultDevice(dev);
                }
            }
            else if (a is SetAppVolumeAction)
            {
                var action = (SetAppVolumeAction)a;

                var device = (action.Device?.Id == null) ? 
                    playbackMgr.Default : playbackMgr.Devices.FirstOrDefault(d => d.Id == action.Device.Id);
                if (device != null)
                {
                    foreach (var app in device.Groups.Where(app => app.AppId == action.App.Id))
                    {
                        DoAudioAction(action.Option, app, action);
                    }
                }
            }
            else if (a is SetDeviceVolumeAction)
            {
                var action = (SetDeviceVolumeAction)a;
                var device = (action.Device?.Id == null) ?
                    playbackMgr.Default : playbackMgr.Devices.FirstOrDefault(d => d.Id == action.Device.Id);
                if (device != null)
                {
                    DoAudioAction(action.Option, device, action);
                }
            }
            else if (a is SetThemeAction)
            {
                var action = (SetThemeAction)a;
                var svc = (dynamic)ServiceBus.Get("EarTrumpet-Themes");
                if (svc != null)
                {
                    svc.Theme = action.Theme;
                }
            }
            else if (a is SetAdditionalSettingsAction)
            {
                DoSetAddonEarTrumpetSettingsAction((SetAdditionalSettingsAction)a);
            }
            else throw new NotImplementedException();
        }

        private static void DoSetAddonEarTrumpetSettingsAction(SetAdditionalSettingsAction action)
        {
            var addonValues = ServiceBus.GetMany(KnownServices.ValueService);
            if (addonValues != null)
            {
                var values = addonValues.Where(v => v is IValue<bool>).Select(v => (IValue<bool>)v).ToList();
                var value = values.FirstOrDefault(v => v.Id == action.SettingId);
                if (value != null)
                {
                    value.Value = action.Value;
                }
            }
        }

        private static void DoAudioAction(StreamActionKind action, IStreamWithVolumeControl stream, IPartWithVolume part)
        {
            switch (action)
            {
                case StreamActionKind.Mute:
                    stream.IsMuted = true;
                    break;
                case StreamActionKind.ToggleMute:
                    stream.IsMuted = !stream.IsMuted;
                    break;
                case StreamActionKind.Unmute:
                    stream.IsMuted = false;
                    break;
                case StreamActionKind.SetVolume:
                    stream.Volume = (float)(part.Volume / 100f);
                    break;
                case StreamActionKind.Increment5:
                    stream.Volume += 0.05f;
                    break;
                case StreamActionKind.Decrement5:
                    stream.Volume -= 0.05f;
                    break;
            }
        }
    }
}
