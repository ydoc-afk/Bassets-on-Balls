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
for checking different window sizes: `Godot --path src --resolution 1080x2340`.
