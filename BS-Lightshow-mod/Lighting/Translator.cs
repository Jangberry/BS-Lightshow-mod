using CustomJSONData;
using CustomJSONData.CustomBeatmap;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BS_Lightshow_mod.Lighting
{
    class Translator
    {
        private static byte ByteClamp(int value)
        {
            return (byte)(value < 0 ? 0 : value > 255 ? 255 : value);
        }
        public static void HandleBeatmapEventCallback(BeatmapEventData eventData)
        {   // TODO: Optimize
            if ((int)eventData.type < 6)
            {
                CustomBeatmapEventData customEventData = eventData as CustomBeatmapEventData;

                byte[] message = new byte[0];

                message = message.Append(ByteClamp(7 + ((int)eventData.type << 3))).ToArray(); // the 3 first bit at 1 (=7) represents a game event 
                message = message.Append(ByteClamp(eventData.value)).ToArray();

                if (customEventData.customData.ContainsKey("_color"))
                {
                    message[0] = ByteClamp(message[0] + (1 << 6));
                    message = message.Concat(customEventData.customData.Get<List<object>>("_color").Take(3).Select(n => ByteClamp((int)(Convert.ToSingle(n) * 255)))).ToArray();
                }
                else if (customEventData.customData.ContainsKey("_lightGradient"))
                {
                    Dictionary<string, object> gradient = customEventData.customData.Get<Dictionary<string, object>>("_lightGradient");

                    message[0] = ByteClamp(message[0] + (2 << 6));
                    byte easing;    // Easing may be void so we must try-catch
                    try { easing = ByteClamp((int)Enum.Parse(typeof(Easings), gradient.Get<string>("_easing"), true)); }
                    catch (ArgumentException) { easing = 0; }
                    message = message.Append(easing).ToArray();
                    message = message.Concat(BitConverter.GetBytes(gradient.Get<float>("_duration"))).ToArray();
                    message = message.Concat(gradient.Get<List<object>>("_startColor").Take(3).Select(n => ByteClamp((int)(Convert.ToSingle(n) * 255)))).ToArray();
                    message = message.Concat(gradient.Get<List<object>>("_endColor").Take(3).Select(n => ByteClamp((int)(Convert.ToSingle(n) * 255)))).ToArray();
                }
                bool end = Plugin.callbackData.nextEventIndex > 0 &&
                    Plugin.beatmapData.beatmapEventsData[Plugin.callbackData.nextEventIndex].time != Plugin.beatmapData.beatmapEventsData[Plugin.callbackData.nextEventIndex - 1].time;
                Plugin.connection.SendStack(message, end);
            }
        }
        public static void NewMap(IDifficultyBeatmap data)
        {
            CustomBeatmapData customData = data.beatmapData as CustomBeatmapData;

            Dictionary<string, object> colorLeft = customData.beatmapCustomData.Get<Dictionary<string, object>>("_envColorLeft");
            Dictionary<string, object> colorRight = customData.beatmapCustomData.Get<Dictionary<string, object>>("_envColorRight");
            if (colorLeft == null || colorRight == null)
            {
#if DEBUG
                Plugin.Log?.Debug("No custom environement colors or unable to parse them");
#endif
                Plugin.connection.Send(new byte[] { 0x01, 0x00, 0x00, 0xFF,
                                                    0x41, 0xFF, 0x00, 0x00}, false);
            }
            else
            {
                Plugin.connection.Send(new byte[] { 0x01, ByteClamp((int)(colorLeft.Get<float>("r")  * 255)), ByteClamp((int)(colorLeft.Get<float>("g")  * 255)), ByteClamp((int)(colorLeft.Get<float>("b")  * 255)),
                                                    0x41, ByteClamp((int)(colorRight.Get<float>("r") * 255)), ByteClamp((int)(colorRight.Get<float>("g") * 255)), ByteClamp((int)(colorRight.Get<float>("b") * 255))}, false);
#if DEBUG
                Plugin.Log?.Debug("Custom environement colors detected and sent");
#endif
            }
        }
        public static void Start()
        {
            Plugin.connection.Send(new byte[] { 0x07, 0x03 }, false);
        }

        enum Easings
        {
            easeLinear = 0,
            easeInSine = 1,
            easeOutSine = 2,
            easeInOutSine = 3,
            easeInCubic = 4,
            easeOutCubic = 5,
            easeInOutCubic = 6,
            easeInQuint = 7,
            easeOutQuint = 8,
            easeInOutQuint = 9,
            easeInCirc = 10,
            easeOutCirc = 11,
            easeInOutCirc = 12,
            easeInElastic = 13,
            easeOutElastic = 14,
            easeInOutElastic = 15,
            easeInQuad = 16,
            easeOutQuad = 17,
            easeInOutQuad = 18,
            easeInQuart = 19,
            easeOutQuart = 20,
            easeInOutQuart = 21,
            easeInExpo = 22,
            easeOutExpo = 23,
            easeInOutExpo = 24,
            easeInBack = 25,
            easeOutBack = 26,
            easeInOutBack = 27,
            easeInBounce = 28,
            easeOutBounce = 29,
            easeInOutBounce = 30
        }
    }
}
