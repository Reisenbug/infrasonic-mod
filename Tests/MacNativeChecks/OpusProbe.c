#include <opus.h>

int infrasonic_get_bitrate(OpusEncoder *encoder)
{
    int bitrate = 0;
    int result = opus_encoder_ctl(encoder, OPUS_GET_BITRATE(&bitrate));
    return result == OPUS_OK ? bitrate : result;
}

int infrasonic_set_bitrate(OpusEncoder *encoder, int bitrate)
{
    return opus_encoder_ctl(encoder, OPUS_SET_BITRATE(bitrate));
}
