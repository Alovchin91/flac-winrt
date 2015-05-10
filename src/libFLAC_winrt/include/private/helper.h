/* libFLAC_winrt - FLAC library for Windows Runtime
 * Copyright (C) 2014-2015  Alexander Ovchinnikov
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

#ifndef FLACRT__PRIVATE__HELPER_H
#define FLACRT__PRIVATE__HELPER_H


struct Helper
{
private:
	static inline Windows::Storage::Streams::IBuffer^ pack_sample_8(const int* const data[], unsigned blocksize, unsigned channels)
	{
		Windows::Storage::Streams::IDataWriter^ dataWriter = ref new Windows::Storage::Streams::DataWriter();
		for (unsigned i = 0; i < blocksize; i++) {
			for (unsigned j = 0; j < channels; j++) {
				dataWriter->WriteByte((FLAC__byte)(data[j][i] + 0x80));
			}
		}
		return dataWriter->DetachBuffer();
	}

	static inline Windows::Storage::Streams::IBuffer^ pack_sample_16(const int* const data[], unsigned blocksize, unsigned channels)
	{
		Windows::Storage::Streams::IDataWriter^ dataWriter = ref new Windows::Storage::Streams::DataWriter();
		for (unsigned i = 0; i < blocksize; i++) {
			for (unsigned j = 0; j < channels; j++) {
				dataWriter->WriteByte((FLAC__byte)(data[j][i] & 0xFF));
				dataWriter->WriteByte((FLAC__byte)((data[j][i] >> 8) & 0xFF));
			}
		}
		return dataWriter->DetachBuffer();
	}

	static inline Windows::Storage::Streams::IBuffer^ pack_sample_24(const int* const data[], unsigned blocksize, unsigned channels)
	{
		Windows::Storage::Streams::IDataWriter^ dataWriter = ref new Windows::Storage::Streams::DataWriter();
		for (unsigned i = 0; i < blocksize; i++) {
			for (unsigned j = 0; j < channels; j++) {
				dataWriter->WriteByte((FLAC__byte)(data[j][i] & 0xFF));
				dataWriter->WriteByte((FLAC__byte)((data[j][i] >> 8) & 0xFF));
				dataWriter->WriteByte((FLAC__byte)((data[j][i] >> 16) & 0xFF));
			}
		}
		return dataWriter->DetachBuffer();
	}

	static inline Windows::Storage::Streams::IBuffer^ pack_sample_32(const int* const data[], unsigned blocksize, unsigned channels)
	{
		Windows::Storage::Streams::IDataWriter^ dataWriter = ref new Windows::Storage::Streams::DataWriter();
		for (unsigned i = 0; i < blocksize; i++) {
			for (unsigned j = 0; j < channels; j++) {
				dataWriter->WriteByte((FLAC__byte)(data[j][i] & 0xFF));
				dataWriter->WriteByte((FLAC__byte)((data[j][i] >> 8) & 0xFF));
				dataWriter->WriteByte((FLAC__byte)((data[j][i] >> 16) & 0xFF));
				dataWriter->WriteByte((FLAC__byte)((data[j][i] >> 24) & 0xFF));
			}
		}
		return dataWriter->DetachBuffer();
	}

public:
	static inline Windows::Storage::Streams::IBuffer^ pack_sample(const int* const data[], unsigned blocksize, unsigned channels, unsigned bits_per_sample)
	{
		switch (bits_per_sample) {
		case 8:
			return pack_sample_8(data, blocksize, channels);
		case 16:
			return pack_sample_16(data, blocksize, channels);
		case 24:
			return pack_sample_24(data, blocksize, channels);
		case 32:
			return pack_sample_32(data, blocksize, channels);
		default:
			throw ref new Platform::InvalidArgumentException("Invalid bits per sample count.");
		}
	}
};


#endif
