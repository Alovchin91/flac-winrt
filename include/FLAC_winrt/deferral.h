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

#ifndef FLACRT__DEFERRAL_H
#define FLACRT__DEFERRAL_H

#include <atomic>
#include <concrt.h>


namespace FLAC {

	namespace WindowsRuntime {

		namespace Decoder {

			namespace Callbacks {

				public interface struct IDeferral
				{
					void Complete();
				};


				class CountdownEvent
				{
				public:
					CountdownEvent(int count) :
						count_(count)
					{
					}

					void AddCount()
					{
						if (!ModifyCount(1)) {
							throw ref new Platform::COMException(E_NOT_VALID_STATE);
						}
					}

					void Signal()
					{
						if (!ModifyCount(-1)) {
							throw ref new Platform::COMException(E_NOT_VALID_STATE);
						}
					}

					void Wait()
					{
						event_.wait();
					}

				private:
					bool ModifyCount(int signalCount)
					{
						int oldCount = (int)count_;
						if (0 == oldCount) {
							return false;
						}

						int newCount = oldCount + signalCount;
						if (newCount < 0) {
							return false;
						}

						count_ = newCount;
						if (0 == newCount) {
							event_.set();
						}

						return true;
					}

					Concurrency::event event_;
					std::atomic<int> count_;
				};


				ref class Deferral sealed : public IDeferral
				{
				internal:
					Deferral(CountdownEvent *count) : count_(count)
					{
					}

				public:
					virtual void Complete()
					{
						if (nullptr != count_) {
							count_->Signal();
							count_ = nullptr;
						}
					}

				private:
					CountdownEvent *count_;
				};


				class DeferralManager
				{
				public:
					DeferralManager() : count_(nullptr)
					{
					}

					IDeferral^ GetDeferral()
					{
						if (nullptr == count_) {
							count_ = new CountdownEvent(1);
						}

						IDeferral^ deferral = ref new Deferral(count_);
						count_->AddCount();

						return deferral;
					}

					void SignalAndWait()
					{
						if (nullptr != count_) {
							count_->Signal();
							count_->Wait();
						}
					}

					virtual ~DeferralManager()
					{
						if (nullptr != count_) {
							delete count_;
							count_ = nullptr;
						}
					}

				private:
					CountdownEvent *count_;
				};

			}
		}
	}
}

#endif
