# Wolf-UI
A UI for Wolf, the main entrypoint when starting a streaming session 

----
### Supported Environment Args
- WOLF_UI_ARGS 

Append arguments to the Wolf-UI launch command.
To force Wolf-UI to use the OpenGl3 backend use `WOLF_UI_ARGS=--rendering-method gl_compatibility --rendering-driver opengl3`

- WOLF_SOCKET_PATH 

Set the path where Wolf-UI will look for the socket

- LOGLEVEL

Set the loglevel, valid arguments: `NONE` `ERROR` `WARNING` `WARN` `INFORMATION` `INFO` `DEBUG`

- WOLF_UI_EFFECTS

`auto` (default), `full` or `reduced`. Full has live-blur glass, an animated backdrop and 60 fps. Reduced uses flat
translucent glass and a still backdrop, so the UI draws nothing while idle and the stream carries a static picture
(less load on a shared GPU, and a lower bitrate for weak clients and Wi-Fi). Auto picks reduced when the renderer is
software, integrated or mobile (llvmpipe, Pi V3D, Mali, ...) or the stream is under 50 Hz, and steps down to reduced
if the UI can't hold its frame rate for a few seconds. It never steps back up on its own. `full` and `reduced` are
never changed automatically.

- WOLF_UI_AUTOUPDATE

if set to `True` then Wolf-UI will ask for a pull of the latest Wolf-UI image on start. Still WIP for none stable releases

---
### Special thanks to: 
- [THOSE AWESOME GUYS](https://thoseawesomeguys.com/) for their awsome [icon pack](https://thoseawesomeguys.com/prompts/)

---
### Developing without a Heeler server
`scripts/mock_api.py` fakes Heeler's control API so the UI can be run and tweaked on its own:

```bash
python3 scripts/mock_api.py /tmp/heeler-mock.sock &
WOLF_SOCKET_PATH=/tmp/heeler-mock.sock Godot --path src
```

`BASSETS_SHOT=/tmp/shot.png BASSETS_SCREEN=apps` (or `users`) saves a screenshot of that screen and quits, which is handy
for checking different window sizes: `Godot --path src --resolution 1080x2340`. `BASSETS_STATS=1` prints frames
rendered and GPU time per second.
