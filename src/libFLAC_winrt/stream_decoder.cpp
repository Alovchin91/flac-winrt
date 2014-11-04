/* libFLAC_winrt - FLAC library for Windows Runtime
 * Copyright (C) 2014  Alexander Ovchinnikov
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 *
 * - Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 *
 * - Neither the name of copyright holder nor the names of project's
 * contributors may be used to endorse or promote products derived from
 * this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
 * PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT HOLDER
 * OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 * PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

#include "FLAC_winrt/decoder.h"
#include "FLAC/assert.h"
#include "private/pack_sample.h"

#include <ppltasks.h>


namespace FLAC {

	namespace WindowsRuntime {

		namespace Decoder {

			StreamDecoder::StreamDecoder() :
				decoder_(::FLAC__stream_decoder_new()), file_stream_(nullptr)
			{ }

			StreamDecoder::~StreamDecoder()
			{
				if (0 != decoder_) {
					(void)::FLAC__stream_decoder_finish(decoder_);
					::FLAC__stream_decoder_delete(decoder_);
				}

				if (nullptr != file_reader_) {
					(void)file_reader_->DetachStream();
					delete file_reader_;
				}
			}

			bool StreamDecoder::IsValid::get()
			{
				return 0 != decoder_ && ReferenceEquals(file_stream_, nullptr) == ReferenceEquals(file_reader_, nullptr);
			}

			bool StreamDecoder::SetOggSerialNumber(int value)
			{
				FLAC__ASSERT(IsValid);
				return !!(::FLAC__stream_decoder_set_ogg_serial_number(decoder_, value));
			}

			bool StreamDecoder::SetMd5Checking(bool value)
			{
				FLAC__ASSERT(IsValid);
				return !!(::FLAC__stream_decoder_set_md5_checking(decoder_, value));
			}

			bool StreamDecoder::SetMetadataRespond(Format::MetadataType type)
			{
				FLAC__ASSERT(IsValid);
				return !!(::FLAC__stream_decoder_set_metadata_respond(decoder_, (::FLAC__MetadataType)(int)type));
			}

			bool StreamDecoder::SetMetadataRespondApplication(const Platform::Array<FLAC__byte>^ id)
			{
				FLAC__ASSERT(IsValid);
				return !!(::FLAC__stream_decoder_set_metadata_respond_application(decoder_, id->Data));
			}

			bool StreamDecoder::SetMetadataRespondAll()
			{
				FLAC__ASSERT(IsValid);
				return !!(::FLAC__stream_decoder_set_metadata_respond_all(decoder_));
			}

			bool StreamDecoder::SetMetadataIgnore(Format::MetadataType type)
			{
				FLAC__ASSERT(IsValid);
				return !!(::FLAC__stream_decoder_set_metadata_ignore(decoder_, (::FLAC__MetadataType)(int)type));
			}

			bool StreamDecoder::SetMetadataIgnoreApplication(const Platform::Array<FLAC__byte>^ id)
			{
				FLAC__ASSERT(IsValid);
				return !!(::FLAC__stream_decoder_set_metadata_ignore_application(decoder_, id->Data));
			}

			bool StreamDecoder::SetMetadataIgnoreAll()
			{
				FLAC__ASSERT(IsValid);
				return !!(::FLAC__stream_decoder_set_metadata_ignore_all(decoder_));
			}

			StreamDecoderState StreamDecoder::GetState()
			{
				FLAC__ASSERT(IsValid);
				return (StreamDecoderState)(int)::FLAC__stream_decoder_get_state(decoder_);
			}

			bool StreamDecoder::GetMd5Checking()
			{
				FLAC__ASSERT(IsValid);
				return !!(::FLAC__stream_decoder_get_md5_checking(decoder_));
			}

			FLAC__uint64 StreamDecoder::GetTotalSamples()
			{
				FLAC__ASSERT(IsValid);
				return ::FLAC__stream_decoder_get_total_samples(decoder_);
			}

			unsigned StreamDecoder::GetChannels()
			{
				FLAC__ASSERT(IsValid);
				return ::FLAC__stream_decoder_get_channels(decoder_);
			}

			Format::Frames::ChannelAssignment StreamDecoder::GetChannelAssignment()
			{
				FLAC__ASSERT(IsValid);
				return (Format::Frames::ChannelAssignment)(int)::FLAC__stream_decoder_get_channel_assignment(decoder_);
			}

			unsigned StreamDecoder::GetBitsPerSample()
			{
				FLAC__ASSERT(IsValid);
				return ::FLAC__stream_decoder_get_bits_per_sample(decoder_);
			}

			unsigned StreamDecoder::GetSampleRate()
			{
				FLAC__ASSERT(IsValid);
				return ::FLAC__stream_decoder_get_sample_rate(decoder_);
			}

			unsigned StreamDecoder::GetBlocksize()
			{
				FLAC__ASSERT(IsValid);
				return ::FLAC__stream_decoder_get_blocksize(decoder_);
			}

			bool StreamDecoder::GetDecodePosition(FLAC__uint64 *position)
			{
				FLAC__ASSERT(IsValid);
				return !!(::FLAC__stream_decoder_get_decode_position(decoder_, position));
			}

			StreamDecoderInitStatus StreamDecoder::Init()
			{
				FLAC__ASSERT(IsValid);
				return (StreamDecoderInitStatus)(int)::FLAC__stream_decoder_init_stream(decoder_, read_callback_, seek_callback_, tell_callback_, length_callback_, eof_callback_, write_callback_, metadata_callback_, error_callback_, /*client_data=*/(void*)this);
			}

			StreamDecoderInitStatus StreamDecoder::Init(Windows::Storage::Streams::IRandomAccessStream^ fileStream)
			{
				FLAC__ASSERT(IsValid);
				file_stream_ = fileStream;
				file_stream_->Seek(0);
				file_reader_ = ref new Windows::Storage::Streams::DataReader(file_stream_);
				return (StreamDecoderInitStatus)(int)::FLAC__stream_decoder_init_stream(decoder_, read_callback_, seek_callback_, tell_callback_, length_callback_, eof_callback_, write_callback_, metadata_callback_, error_callback_, /*client_data=*/(void*)this);
			}

			StreamDecoderInitStatus StreamDecoder::InitOgg()
			{
				FLAC__ASSERT(IsValid);
				return (StreamDecoderInitStatus)(int)::FLAC__stream_decoder_init_ogg_stream(decoder_, read_callback_, seek_callback_, tell_callback_, length_callback_, eof_callback_, write_callback_, metadata_callback_, error_callback_, /*client_data=*/(void*)this);
			}

			StreamDecoderInitStatus StreamDecoder::InitOgg(Windows::Storage::Streams::IRandomAccessStream^ fileStream)
			{
				FLAC__ASSERT(IsValid);
				file_stream_ = fileStream;
				file_stream_->Seek(0);
				file_reader_ = ref new Windows::Storage::Streams::DataReader(file_stream_);
				return (StreamDecoderInitStatus)(int)::FLAC__stream_decoder_init_ogg_stream(decoder_, read_callback_, seek_callback_, tell_callback_, length_callback_, eof_callback_, write_callback_, metadata_callback_, error_callback_, /*client_data=*/(void*)this);
			}

			bool StreamDecoder::Finish()
			{
				FLAC__ASSERT(IsValid);
				if (nullptr != file_reader_) {
					(void)file_reader_->DetachStream();
					delete file_reader_;
				}
				file_reader_ = nullptr;
				file_stream_ = nullptr;
				return !!(::FLAC__stream_decoder_finish(decoder_));
			}

			bool StreamDecoder::Flush()
			{
				FLAC__ASSERT(IsValid);
				return !!(::FLAC__stream_decoder_flush(decoder_));
			}

			bool StreamDecoder::Reset()
			{
				FLAC__ASSERT(IsValid);
				return !!(::FLAC__stream_decoder_reset(decoder_));
			}

			bool StreamDecoder::ProcessSingle()
			{
				FLAC__ASSERT(IsValid);
				return !!(::FLAC__stream_decoder_process_single(decoder_));
			}

			bool StreamDecoder::ProcessUntilEndOfMetadata()
			{
				FLAC__ASSERT(IsValid);
				return !!(::FLAC__stream_decoder_process_until_end_of_metadata(decoder_));
			}

			bool StreamDecoder::ProcessUntilEndOfStream()
			{
				FLAC__ASSERT(IsValid);
				return !!(::FLAC__stream_decoder_process_until_end_of_stream(decoder_));
			}

			bool StreamDecoder::SkipSingleFrame()
			{
				FLAC__ASSERT(IsValid);
				return !!(::FLAC__stream_decoder_skip_single_frame(decoder_));
			}

			bool StreamDecoder::SeekAbsolute(FLAC__uint64 sample)
			{
				FLAC__ASSERT(IsValid);
				return !!(::FLAC__stream_decoder_seek_absolute(decoder_, sample));
			}

			::FLAC__StreamDecoderReadStatus StreamDecoder::read_callback_(const ::FLAC__StreamDecoder *decoder, FLAC__byte buffer[], size_t *bytes, void *client_data)
			{
				(void)decoder;
				FLAC__ASSERT(0 != client_data);
				StreamDecoder^ instance = reinterpret_cast<StreamDecoder^>(client_data);
				FLAC__ASSERT(nullptr != instance && instance->IsValid);
				return nullptr != instance->file_stream_
					? instance->stream_read_callback_(buffer, bytes)
					: (::FLAC__StreamDecoderReadStatus)(int)instance->ReadCallback(Platform::ArrayReference<FLAC__byte>(buffer, *bytes), bytes);
			}

			::FLAC__StreamDecoderSeekStatus StreamDecoder::seek_callback_(const ::FLAC__StreamDecoder *decoder, FLAC__uint64 absolute_byte_offset, void *client_data)
			{
				(void)decoder;
				FLAC__ASSERT(0 != client_data);
				StreamDecoder^ instance = reinterpret_cast<StreamDecoder^>(client_data);
				FLAC__ASSERT(nullptr != instance && instance->IsValid);
				return nullptr != instance->file_stream_
					? instance->stream_seek_callback_(absolute_byte_offset)
					: (::FLAC__StreamDecoderSeekStatus)(int)instance->SeekCallback(absolute_byte_offset);
			}

			::FLAC__StreamDecoderTellStatus StreamDecoder::tell_callback_(const ::FLAC__StreamDecoder *decoder, FLAC__uint64 *absolute_byte_offset, void *client_data)
			{
				(void)decoder;
				FLAC__ASSERT(0 != client_data);
				StreamDecoder^ instance = reinterpret_cast<StreamDecoder^>(client_data);
				FLAC__ASSERT(nullptr != instance && instance->IsValid);
				return nullptr != instance->file_stream_
					? instance->stream_tell_callback_(absolute_byte_offset)
					: (::FLAC__StreamDecoderTellStatus)(int)instance->TellCallback(absolute_byte_offset);
			}

			::FLAC__StreamDecoderLengthStatus StreamDecoder::length_callback_(const ::FLAC__StreamDecoder *decoder, FLAC__uint64 *stream_length, void *client_data)
			{
				(void)decoder;
				FLAC__ASSERT(0 != client_data);
				StreamDecoder^ instance = reinterpret_cast<StreamDecoder^>(client_data);
				FLAC__ASSERT(nullptr != instance && instance->IsValid);
				return nullptr != instance->file_stream_
					? instance->stream_length_callback_(stream_length)
					: (::FLAC__StreamDecoderLengthStatus)(int)instance->LengthCallback(stream_length);
			}

			FLAC__bool StreamDecoder::eof_callback_(const ::FLAC__StreamDecoder *decoder, void *client_data)
			{
				(void)decoder;
				FLAC__ASSERT(0 != client_data);
				StreamDecoder^ instance = reinterpret_cast<StreamDecoder^>(client_data);
				FLAC__ASSERT(nullptr != instance && instance->IsValid);
				return nullptr != instance->file_stream_
					? instance->stream_eof_callback_()
					: (instance->EofCallback() ? TRUE : FALSE);
			}

			::FLAC__StreamDecoderWriteStatus StreamDecoder::write_callback_(const ::FLAC__StreamDecoder *decoder, const ::FLAC__Frame *frame, const FLAC__int32 * const buffer[], void *client_data)
			{
				(void)decoder;
				FLAC__ASSERT(0 != client_data);
				StreamDecoder^ instance = reinterpret_cast<StreamDecoder^>(client_data);
				FLAC__ASSERT(nullptr != instance && instance->IsValid);
				Callbacks::StreamDecoderWriteBuffer^ write_buffer = ref new Callbacks::StreamDecoderWriteBuffer(buffer, frame->header);
				return (::FLAC__StreamDecoderWriteStatus)(int)instance->WriteCallback(ref new Format::Frame(frame), write_buffer);
			}

			void StreamDecoder::metadata_callback_(const ::FLAC__StreamDecoder *decoder, const ::FLAC__StreamMetadata *metadata, void *client_data)
			{
				(void)decoder;
				FLAC__ASSERT(0 != client_data);
				StreamDecoder^ instance = reinterpret_cast<StreamDecoder^>(client_data);
				FLAC__ASSERT(nullptr != instance && instance->IsValid);
				instance->MetadataCallback(ref new Format::StreamMetadata(metadata));
			}

			void StreamDecoder::error_callback_(const ::FLAC__StreamDecoder *decoder, ::FLAC__StreamDecoderErrorStatus status, void *client_data)
			{
				(void)decoder;
				FLAC__ASSERT(0 != client_data);
				StreamDecoder^ instance = reinterpret_cast<StreamDecoder^>(client_data);
				FLAC__ASSERT(nullptr != instance && instance->IsValid);
				instance->ErrorCallback((Callbacks::StreamDecoderErrorStatus)(int)status);
			}

			::FLAC__StreamDecoderReadStatus StreamDecoder::stream_read_callback_(FLAC__byte buffer[], size_t *bytes)
			{
				if (*bytes > 0) {
					*bytes = concurrency::create_task(file_reader_->LoadAsync(*bytes)).get();
					if (*bytes == 0)
						return FLAC__STREAM_DECODER_READ_STATUS_END_OF_STREAM;
					file_reader_->ReadBytes(Platform::ArrayReference<FLAC__byte>(buffer, *bytes));
					return FLAC__STREAM_DECODER_READ_STATUS_CONTINUE;
				}
				return FLAC__STREAM_DECODER_READ_STATUS_ABORT;
			}

			::FLAC__StreamDecoderSeekStatus StreamDecoder::stream_seek_callback_(FLAC__uint64 absolute_byte_offset)
			{
				if (absolute_byte_offset > file_stream_->Size)
					return FLAC__STREAM_DECODER_SEEK_STATUS_ERROR;
				file_stream_->Seek(absolute_byte_offset);
				return FLAC__STREAM_DECODER_SEEK_STATUS_OK;
			}

			::FLAC__StreamDecoderTellStatus StreamDecoder::stream_tell_callback_(FLAC__uint64 *absolute_byte_offset)
			{
				*absolute_byte_offset = file_stream_->Position;
				return FLAC__STREAM_DECODER_TELL_STATUS_OK;
			}

			::FLAC__StreamDecoderLengthStatus StreamDecoder::stream_length_callback_(FLAC__uint64 *stream_length)
			{
				*stream_length = file_stream_->Size;
				return FLAC__STREAM_DECODER_LENGTH_STATUS_OK;
			}

			FLAC__bool StreamDecoder::stream_eof_callback_()
			{
				return (file_stream_->Position < file_stream_->Size) ? FALSE : TRUE;
			}


			namespace Callbacks {

				Windows::Storage::Streams::IBuffer^ StreamDecoderWriteBuffer::GetBuffer()
				{
					if (!buffer_) {
						uint32 count = frame_header_.blocksize * ((frame_header_.channels * frame_header_.bits_per_sample) >> 3);
						buffer_ = ref new Windows::Storage::Streams::Buffer(count);
						buffer_->Length = pack_sample(data_, frame_header_.blocksize, frame_header_.channels, buffer_, frame_header_.bits_per_sample);
					}
					return buffer_;
				}

				Platform::Array<FLAC__int32>^ StreamDecoderWriteBuffer::GetData(unsigned index)
				{
					if (index > frame_header_.channels)
						throw ref new Platform::OutOfBoundsException();

					if (!data_array_) {
						data_array_ = ref new Platform::Array<Platform::Object^>(frame_header_.channels);
						for (unsigned i = 0; i < frame_header_.channels; i++) {
							data_array_[i] = ref new Platform::Array<FLAC__int32>(const_cast<FLAC__int32 *>(data_[i]), frame_header_.blocksize);
						}
					}
					return safe_cast<Platform::Array<FLAC__int32>^>(data_array_[index]);
				}

			}

		}
	}
}
