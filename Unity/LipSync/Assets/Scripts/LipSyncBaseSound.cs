using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NPinyin;
//using Microsoft.International.Converters.PinYinConverter;

public class LipSyncBaseSound : MonoBehaviour
{
    public SkinnedMeshRenderer Source;

    public AnimationCurve smoothCurve;
    public float smoothSpeed = 0.2f;

    public AnimationCurve EyeCurve;

    [System.Serializable]
    public class Result
    {
        public float conf;
        public float end;
        public float start;
        public string word;
    }

    [System.Serializable]
    public class Vosk
    {
        public Result[] result;
        public string text;
    }

    
    public List<TextAsset> jsonFiles;

    private List<Result> singleCharacter = new List<Result>();

    //public AnimationCurve[] Curve = new AnimationCurve[5];

    public AudioSource Audio;
    public List<AudioClip> Clips;
    private int AudioIndex = 0;

    private bool isPlayingAudio = false;

    private float[] MouthBlendShape = new float[5] {0.0f, 0.0f, 0.0f, 0.0f, 0.0f };// a i u e o
    private float sum = 1.0f;

    private class Data
    {
        public float start;
        public float end;
        public float[] yunmu = new float[6] { 0.0f , 0.0f ,0.0f ,0.0f, 0.0f ,0.0f};
        
    }

    //private List<Data> KouXing = new List<Data>();
    private List<List<Data>> KouXings = new List<List<Data>>();

    // Start is called before the first frame update
    void Start()
    {
        KouXings.Clear();

        for (int i = 0;i < jsonFiles.Count; i++)
        {
            Analysis(jsonFiles[i]);
        }

    }

    // Update is called once per frame
    void Update()
    {
                
        sum = 1.0f;

        List<Data> KouXing = KouXings[AudioIndex];

        //如果正在播放音频，那么根据List中的音素去累加对应的口型的BlendShape的权重
        if (!Audio.isPlaying)
        {
            isPlayingAudio = false;

            for (int n = 0; n < 5; n++)
            {

                MouthBlendShape[n] = Mathf.Clamp01(MouthBlendShape[n] - smoothSpeed * Time.deltaTime);
                

            }
        }
        else
        {
            bool isFind = false;

            //Debug.Log(Audio.time);
            
            for (int i = 0; i < KouXing.Count; i++)
            {
                                
                if ((Audio.time < KouXing[i].start) || (Audio.time > KouXing[i].end))
                {
                    continue;
                }
                else
                {
                    float tempsum = 0.0f;
                    
                    for (int n = 0; n < 5; n++)
                    {
                        if (KouXing[i].yunmu[n] > 0.0f)
                        {
                            MouthBlendShape[n] = Mathf.Clamp01(MouthBlendShape[n] + smoothSpeed * Time.deltaTime);
                            
                        }
                        else
                        {
                            MouthBlendShape[n] = Mathf.Clamp01(MouthBlendShape[n] - smoothSpeed * Time.deltaTime * 1.0f);
                            
                        }

                        tempsum += KouXing[i].yunmu[n];
                    }

                    if (tempsum > 1.0f)
                    {
                        sum = Mathf.Sqrt(tempsum);
                    }

                    //Debug.Log(Audio.time);

                    isFind = true;
                    break;
                }
            }
            
            if (!isFind)
            {
                for (int n = 0; n < 5; n++)
                {
                                       
                    MouthBlendShape[n] = Mathf.Clamp01(MouthBlendShape[n] - smoothSpeed * Time.deltaTime * 0.6f);
                    

                }
            }
        }

        SetWeight();
    }

    public void PlayAudio(int index)
    {
        if (!isPlayingAudio)
        {
            AudioIndex = index;
            Audio.clip = Clips[index];
            Audio.Play();
            isPlayingAudio = true;
            
        }
        
    }

    private void SetWeight()
    {
        Source.SetBlendShapeWeight(19, smoothCurve.Evaluate(MouthBlendShape[0]) * 100.0f / sum);//a
        Source.SetBlendShapeWeight(14, smoothCurve.Evaluate(MouthBlendShape[1]) * 100.0f / sum);//i
        Source.SetBlendShapeWeight(16, smoothCurve.Evaluate(MouthBlendShape[2]) * 100.0f / sum);//u
        Source.SetBlendShapeWeight(13, smoothCurve.Evaluate(MouthBlendShape[3]) * 100.0f / sum);//e
        Source.SetBlendShapeWeight(15, smoothCurve.Evaluate(MouthBlendShape[4]) * 100.0f / sum);//o

        float eye = (smoothCurve.Evaluate(MouthBlendShape[0]) * 20.0f + smoothCurve.Evaluate(MouthBlendShape[4]) * 10.0f) / sum;

        float muscle = EyeCurve.Evaluate(MouthBlendShape[0] * 0.1f + MouthBlendShape[4] * 0.1f + MouthBlendShape[1] * 0.1f);
        //Debug.Log(muscle);

        eye = EyeCurve.Evaluate(eye / 100.0f) * 100.0f;

        Source.SetBlendShapeWeight(10, -muscle * 40.0f + Mathf.PerlinNoise(Time.time * 0.5f , 1.0f) * 25.0f + 10.0f);//Brow
        Source.SetBlendShapeWeight(4, -eye * 0.6f);
        Source.SetBlendShapeWeight(5, -eye * 0.6f);

        Source.SetBlendShapeWeight(0, muscle * 100.0f + Mathf.PerlinNoise(Time.time * 0.5f, 0.8f) * 20.0f - 5.0f);


    }
    

    //音频文本识别
    private void Analysis(TextAsset jsonFile)
    {
        singleCharacter.Clear();
        List<Data> KouXing = new List<Data>();

        Vosk VoskData = JsonUtility.FromJson<Vosk>(jsonFile.text);

        //对每个文字循环，先转成拼音，然后根据声母韵母发音来平均分每个文字的时长
        //进而生成一个List，里面是每个音素和对应的开始结束时间
        foreach (Result item in VoskData.result)
        {
            //Debug.Log(item.word.Length + "conf " + item.conf + "end " + item.end + "start " + item.start + "word " + item.word);
            float timestep = (item.end - item.start) / item.word.Length;
            for (int i = 0; i < item.word.Length; i++)
            {
                Result tempresult = new Result();
                tempresult.start = item.start + i * timestep;
                tempresult.end = tempresult.start + timestep;
                tempresult.word = item.word.Substring(i, 1);
                tempresult.conf = item.conf;
                singleCharacter.Add(tempresult);

                //Debug.Log(tempresult.word.Length + "conf " + tempresult.conf + "end " + tempresult.end + "start " + tempresult.start + "word " + tempresult.word + "pinyin" + Pinyin.GetPinyin(tempresult.word) + "Length" + Pinyin.GetPinyin(tempresult.word).Length);

                string pinyin = Pinyin.GetPinyin(tempresult.word);

                int yinsuchangdu = 0;
                List<int> templist = new List<int>();
                for (int n = 0; n < pinyin.Length; n++)
                {
                    switch (pinyin[n])
                    {
                        case 'a':
                            yinsuchangdu++;
                            templist.Add(0);
                            break;
                        case 'i':
                            yinsuchangdu++;
                            templist.Add(1);
                            break;
                        case 'u':
                            yinsuchangdu++;
                            templist.Add(2);
                            break;
                        case 'e':
                            yinsuchangdu++;
                            templist.Add(3);
                            break;
                        case 'o':
                            yinsuchangdu++;
                            templist.Add(4);
                            break;
                        case 'b':
                            yinsuchangdu++;
                            templist.Add(5);
                            break;
                        case 'p':
                            yinsuchangdu++;
                            templist.Add(5);
                            break;
                        case 'm':
                            yinsuchangdu++;
                            templist.Add(5);
                            break;
                        case 'f':
                            yinsuchangdu++;
                            templist.Add(5);
                            break;
                        case 'w':
                            yinsuchangdu++;
                            templist.Add(2);
                            break;
                        case 'y':
                            yinsuchangdu++;
                            templist.Add(1);
                            break;
                        case 'n':
                            yinsuchangdu++;
                            templist.Add(5);
                            break;
                    }
                }


                float yinstep = (tempresult.end - tempresult.start) / yinsuchangdu;
                for (int n = 0; n < yinsuchangdu; n++)
                {
                    Data temp = new Data();
                    temp.yunmu[templist[n]] = 1.0f;
                    temp.start = tempresult.start + n * yinstep;
                    temp.end = temp.start + yinstep;
                    KouXing.Add(temp);
                    //Debug.Log("start" + temp.start + "end" + temp.end + "kouxing" + templist[n]);
                }


            }
        }


        KouXings.Add(KouXing);

    }
}
