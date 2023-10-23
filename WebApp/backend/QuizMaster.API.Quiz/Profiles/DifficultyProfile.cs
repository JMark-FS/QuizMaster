﻿using AutoMapper;
using QuizMaster.API.Quiz.Models;
using QuizMaster.Library.Common.Entities.Questionnaire;

namespace QuizMaster.API.Quiz.Profiles
{
	public class DifficultyProfile : Profile
	{
		public DifficultyProfile()
		{
			CreateMap<QuestionDifficulty, DifficultyDto>();
			CreateMap<DifficultyCreateDto, QuestionDifficulty>();
		}
	}
}
