#include <string.h>
#include <stdio.h>
#include <gst/gst.h>
#include <ctype.h>

typedef struct _CustomData
{
  GstElement *pipeline;
  GstElement *video_sink;
  GstBus *bus;
  GMainLoop *loop;

  gboolean playing;      /* Are we in the PLAYING state? */
  gboolean notify;       /* Should Notify Continue? */
  gint notify_id;       /* Should Notify Continue? */
  gint64 duration;       /* How long does this media last, in nanoseconds */

} CustomData;

/* String Starts With Value */
static gboolean strstarts(const char *str, const char *prefix)
{
  return strncasecmp(str, prefix, strlen(prefix)) == 0;
}


/* Process Messages from GStreamer */
static gboolean handle_message (GstBus *bus, GstMessage *msg, CustomData *data) 
{
  GError *err;
  gchar *debug_info;

  switch (GST_MESSAGE_TYPE (msg)) 
  {
    case GST_MESSAGE_ERROR:
      gst_message_parse_error (msg, &err, &debug_info);
      g_printerr ("ERROR: Received from element %s: %s\n", GST_OBJECT_NAME (msg->src), err->message);
      g_printerr ("Debugging information: %s\n", debug_info ? debug_info : "none");
      g_clear_error (&err);
      g_free (debug_info);
      g_main_loop_quit (data->loop);
      break;

    case GST_MESSAGE_EOS:
      g_print ("END: Exiting Program.\n");
      g_main_loop_quit (data->loop);
      break;

    case GST_MESSAGE_STATE_CHANGED: 
    {
      GstState old_state, new_state, pending_state;
      gst_message_parse_state_changed (msg, &old_state, &new_state, &pending_state);
      
      if (GST_MESSAGE_SRC (msg) == GST_OBJECT (data->pipeline)) 
      {
        if (new_state == GST_STATE_PLAYING) 
        {
            g_print ("STATE: PLAYING\n");
        }
        else if (new_state == GST_STATE_PAUSED) 
        {
            g_print ("STATE: PAUSED\n");
        }
      }

      break;
    } 
  }

  /* We want to keep receiving messages */
  return TRUE;
}

static gboolean print_info(void * dataPtr)
{
  CustomData * data = dataPtr;
  gint64 current = -1;

  /* Query the current position of the stream */
  if (!gst_element_query_position (data->pipeline, GST_FORMAT_TIME, &current)) {
    g_printerr ("ERROR: Could not query current position.\n");
  }

  /* If we didn't know it yet, query the stream duration */
  if (!gst_element_query_duration (data->pipeline, GST_FORMAT_TIME, &data->duration)) {
    g_printerr ("ERROR: Could not query current duration.\n");
  }

  /* Print current position and total duration */
  g_print ("POSITION: %" GST_TIME_FORMAT "\n", GST_TIME_ARGS (current));
  g_print ("DURATION: %" GST_TIME_FORMAT "\n",  GST_TIME_ARGS (data->duration));

  return data->notify;
}

/* Process keyboard input */
static gboolean handle_keyboard (GIOChannel * source, GIOCondition cond, CustomData * data)
{
  gchar *str = NULL;

  if (g_io_channel_read_line (source, &str, NULL, NULL, NULL) != G_IO_STATUS_NORMAL) 
    return TRUE;
  
  if (strstarts(str, "pause")) 
  {
    data->playing = TRUE;
    gst_element_set_state (data->pipeline, GST_STATE_PAUSED);
    g_print ("  NOTE: Setting state to %s\n","PAUSE");
  }
  else if (strstarts(str, "play"))
  {
    data->playing = FALSE;
    gst_element_set_state (data->pipeline, GST_STATE_PLAYING);
    g_print ("  NOTE: Setting state to %s\n",  "PLAYING" );
  }
  else if (strstarts(str, "position:"))
  {
    gint64 val = strtol(str + strlen("position:"), NULL, 0);
  
    float seconds = val / 1000.0f;
    gint64 seekPos = val * 1000000;

    g_print ("  NOTE: Moving to Position %f seconds\n", seconds);
  
    if (!gst_element_seek (data->pipeline, 1.0, GST_FORMAT_TIME, GST_SEEK_FLAG_FLUSH, GST_SEEK_TYPE_SET, seekPos, GST_SEEK_TYPE_NONE, GST_CLOCK_TIME_NONE))  
    {
      g_print ("ERROR: Seek Failed!\n");
    }
  }
  else if (strstarts(str, "notify:"))
  {
    gint64 val = strtol(str + strlen("notify:"), NULL, 0);
    if (val > 0)
    {
      data->notify = TRUE;
      float seconds = val / 1000.0f;

      data->notify_id = g_timeout_add(val, print_info, (void*)data);

      g_print ("  NOTE: Starting Notification every %f seconds\n", seconds);
    }
    else
    {
      g_print ("  NOTE: Stopping Notification\n");
      if (data->notify_id > 0)
      {
        g_source_remove(data->notify_id);
        data->notify = FALSE;
      }
    }

  }
  else if (strstarts(str, "info"))
  {
    print_info(data);
  }
  else if (strstarts(str, "quit"))
  {
    g_main_loop_quit (data->loop);    
    g_print ("  NOTE: Quiting Playback\n");
  }

  g_free (str);

  return TRUE;
}

int main (int argc, char *argv[])
{
  gchar **argvn;
  GError *error = NULL;
  CustomData data;
  GstStateChangeReturn ret;
  GIOChannel *io_stdin;

  /* Initialize GStreamer */
  gst_init (&argc, &argv);

  /* Initialize our data structure */
  memset (&data, 0, sizeof (data));

  /* Print usage map */
  g_print ("\nUSAGE: Choose one of the following options, then press enter:\n"
      "   'PLAY' a Paused Video\n"
      "   'PAUSE' the Video\n"      
      "   'INFO' about the Video Including Duration and Position\n"      
      "   'POSITION:<Milliseconds>' the Re-Positon Video Playback\n"
      "   'NOTIFY:<Milliseconds>' Notify of Duration / Position Periodically\n"
      "   'QUIT' to End Playback and Quit Program\n\n");


  /* Build the pipeline From the Command Line */
  argvn = g_new0 (char *, argc);
  memcpy (argvn, argv + 1, sizeof (char *) * (argc - 1));
  data.pipeline = (GstElement *) gst_parse_launchv ((const gchar **) argvn, &error);
  g_free (argvn);

  /* Add a keyboard watch so we get notified of keystrokes */
  io_stdin = g_io_channel_unix_new (fileno (stdin));
  g_io_add_watch (io_stdin, G_IO_IN, (GIOFunc) handle_keyboard, &data);

  /* Start playing */
  ret = gst_element_set_state (data.pipeline, GST_STATE_PLAYING);
  if (ret == GST_STATE_CHANGE_FAILURE) {
    g_printerr ("Unable to set the pipeline to the playing state.\n");
    gst_object_unref (data.pipeline);
    return -1;
  }

  data.playing = TRUE;

  /* Add a bus watch, so we get notified when a message arrives */
  data.bus = gst_element_get_bus (data.pipeline);
  gst_bus_add_watch ( data.bus, (GstBusFunc)handle_message, &data);
  
  /* Create a GLib Main Loop and set it to run */
  data.loop = g_main_loop_new (NULL, FALSE);
  g_main_loop_run (data.loop);

  /* Free resources */
  g_main_loop_unref (data.loop);
  g_io_channel_unref (io_stdin);
  gst_element_set_state (data.pipeline, GST_STATE_NULL);
  if (data.video_sink != NULL)
    gst_object_unref (data.video_sink);

  gst_object_unref (data.pipeline);
  return 0;
}
