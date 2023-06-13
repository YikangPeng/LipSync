#!/usr/bin/env python3

import wave
import sys

from vosk import Model, KaldiRecognizer, SetLogLevel

# You can set log level to -1 to disable debug messages
SetLogLevel(0)

wf = wave.open(sys.argv[1], "rb")
if wf.getnchannels() != 1 or wf.getsampwidth() != 2 or wf.getcomptype() != "NONE":
    print("Audio file must be WAV format mono PCM.")
    sys.exit(1)

#model = Model(lang="en-us")

# You can also init model by name or with a folder path
# model = Model(model_name="vosk-model-en-us-0.21")
# model = Model("models/en")

model = Model("model")

rec = KaldiRecognizer(model, wf.getframerate())
rec.SetWords(True)
rec.SetPartialWords(True)

while True:
    data = wf.readframes(4000)
    if len(data) == 0:
        break
    if rec.AcceptWaveform(data):
        #print(rec.Result())
        print("e")
            
    else:
        #print(rec.PartialResult())
        print("e")

#print(rec.FinalResult())
            
output = rec.FinalResult()
outputfilename = sys.argv[1]+'.json'
print(output)
with open(outputfilename,mode = 'w',encoding = 'utf-8') as file_obj:
    file_obj.write(output)
