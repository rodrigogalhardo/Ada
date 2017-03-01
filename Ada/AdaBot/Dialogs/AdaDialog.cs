﻿using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using AdaSDK;
using System.Net.Http;
using Microsoft.IdentityModel.Protocols;
using System.Configuration;
using System.Net;
using Newtonsoft.Json;
using AdaSDK.Models;

namespace AdaBot.Dialogs
{
    [Serializable]
    public class AdaDialog : LuisDialog<object>
    {
        //[NonSerialized]
        //private Activity context.Activity;
        //[NonSerialized]
        //private CreateDialog customDialog = new CreateDialog();

        public AdaDialog(params ILuisService[] services) : base(services)
        {

        }

        protected override async Task MessageReceived(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            var message = (Activity)await item;
            await base.MessageReceived(context, item);
        }

        [LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            string message = $"Je n'ai pas compris :/";
            await context.PostAsync(message);
            message = $"Je suis constamment en apprentissage, je vais demander à mes créateurs de m'apprendre ta phrase ;)";
            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }

        [LuisIntent("SayHello")]
        public async Task SayHello(IDialogContext context, LuisResult result)
        {
            string nameUser = context.Activity.From.Name;
            string[] firstNameUser = nameUser.Split(' ');
            string message = $"Bonjour {firstNameUser[0]}";
            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }

        [LuisIntent("GetVisitsToday")]
        public async Task GetVisitsToday(IDialogContext context, LuisResult result)
        {
            AdaClient client = new AdaClient();
            List<VisitDto> visits = await client.GetVisitsToday();

            Activity replyToConversation;

            if (visits.Count == 0)
            {
                replyToConversation = ((Activity)context.Activity).CreateReply("Je n'ai encore vu personne aujourd'hui... :'(");
                replyToConversation.Recipient = context.Activity.From;
                replyToConversation.Type = "message";
            }
            else
            {
                replyToConversation = ((Activity)context.Activity).CreateReply("J'ai vu " + visits.Count + " personnes aujourd'hui! :D");
                replyToConversation.Recipient = context.Activity.From;
                replyToConversation.Type = "message";
                replyToConversation.AttachmentLayout = "carousel";
                replyToConversation.Attachments = new List<Attachment>();

                foreach (var visit in visits)
                {
                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: $"{ ConfigurationManager.AppSettings["WebAppUrl"] }{VirtualPathUtility.ToAbsolute(visit.ProfilePicture.Last().Uri)}")); //A mettre dans le SDK

                    //Calcul la bonne année et la bonne heure.
                    DateTime today = DateTime.Today;
                    int wrongDate = visit.PersonVisit.DateVisit.Year;
                    int goodDate = DateTime.Today.Year - wrongDate;
                    string messageDate = "";
                    string firstname;
                    DateTime visitDate = visit.PersonVisit.DateVisit;

                    //Recherche du prénom de la personne
                    if (visit.PersonVisit.FirstName == null)
                    {
                        firstname = "une personne inconnue";
                    }
                    else
                    {
                        firstname = visit.PersonVisit.FirstName;
                    }

                    var customDialog = new CreateDialog();
                    messageDate = customDialog.GetVisitsMessage(firstname, visitDate);

                    HeroCard plCard = new HeroCard()
                    {
                        Title = visit.PersonVisit.FirstName,
                        Text = messageDate + " (" + Convert.ToString(visit.PersonVisit.DateVisit.AddHours(1).AddYears(goodDate)) + ")",
                        //Subtitle = 
                        Images = cardImages
                        //Buttons = cardButtons
                    };

                    Attachment plAttachment = plCard.ToAttachment();
                    replyToConversation.Attachments.Add(plAttachment);
                }
            }

            await context.PostAsync(replyToConversation);
            context.Wait(MessageReceived);
        }

        [LuisIntent("GetLastVisitPerson")]
        public async Task GetLastVisitPerson(IDialogContext context, LuisResult result)
        {
            string firstname = result.Entities[0].Entity;
            AdaClient client = new AdaClient();
            List<VisitDto> visits = await client.GetLastVisitPerson(firstname);

            Activity replyToConversation;

            if (visits.Count == 0)
            {
                replyToConversation = ((Activity)context.Activity).CreateReply("Je n'ai pas encore rencontré " + firstname + " :/ Il faudrait nous présenter! ^^");
                replyToConversation.Recipient = context.Activity.From;
                replyToConversation.Type = "message";
            }
            else
            {
                replyToConversation = ((Activity)context.Activity).CreateReply("Voyons voir...");
                replyToConversation.Recipient = context.Activity.From;
                replyToConversation.Type = "message";
                replyToConversation.AttachmentLayout = "carousel";
                replyToConversation.Attachments = new List<Attachment>();

                foreach (var visit in visits)
                {
                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: $"{ ConfigurationManager.AppSettings["WebAppUrl"] }{VirtualPathUtility.ToAbsolute(visit.ProfilePicture.Last().Uri)}")); // a mettre dans le SDK

                    //Calcul la bonne année et la bonne heure.
                    DateTime today = DateTime.Today;
                    int wrongDate = visit.PersonVisit.DateVisit.Year;
                    int goodDate = DateTime.Today.Year - wrongDate;
                    string messageDate = "";
                    DateTime visitDate = visit.PersonVisit.DateVisit;

                    var customDialog = new CreateDialog();
                    messageDate = customDialog.GetVisitsMessage(firstname, visitDate);

                    HeroCard plCard = new HeroCard()
                    {
                        Title = visit.PersonVisit.FirstName,
                        Text = messageDate + " (" + Convert.ToString(visit.PersonVisit.DateVisit.AddHours(1).AddYears(goodDate)) + ")",
                        //Subtitle = 
                        Images = cardImages
                        //Buttons = cardButtons
                    };

                    Attachment plAttachment = plCard.ToAttachment();
                    replyToConversation.Attachments.Add(plAttachment);
                }
            }

            await context.PostAsync(replyToConversation);
            context.Wait(MessageReceived);
        }

        [LuisIntent("GetStatsVisits")]
        public async Task GetLastVisGetStatsVisitsitPerson(IDialogContext context, LuisResult result)
        {
            AdaClient client = new AdaClient();
            Activity replyToConversation;
            CreateDialog customDialog = new CreateDialog();

            //Lists for different stats
            //ATTENTION Le tri n'est bassé pour l'instant que sur les visites du jour! => A modifier une fois les dates OK
            List<VisitDto> allvisits = await client.GetVisitsToday();
            List<VisitDto> visitsReturn = new List<VisitDto>();
            List<VisitDto> tmp = allvisits.ToList();

            List<ProfilePictureDto> EmotionPicture = new List<ProfilePictureDto>();
            bool askingEmotion = false;

            int nbVisits = tmp.Count();
            int agePerson;
            string genderReturn = "personnes";
            string ageReturn = "";
            string emotionReturn = "";

            //Single Entities
            int nbEntities = result.Entities.Count();
            for (int i = 0; i < nbEntities; i++)
            {
                //Get actual number of visits return
                if (visitsReturn.Count() != 0)
                {
                    nbVisits = visitsReturn.Count();
                    tmp = visitsReturn.ToList();
                    visitsReturn.Clear();
                }

                if (result.Entities[i].Type == "Gender")
                {
                    string value = result.Entities[i].Entity;
                    GenderValues gender = GenderValues.Male;
                    if (value == "femme" || value == "femmes" || value == "fille" || value == "filles")
                    {
                        gender = GenderValues.Female;
                        genderReturn = " femmes";
                    }
                    else
                    {
                        genderReturn = " hommes";
                    }

                    for (int y = 0; y < nbVisits; y++)
                    {
                        if (tmp[y].PersonVisit.Gender == gender)
                        {
                            visitsReturn.Add(tmp[y]);
                        }
                    }
                }
                else if (result.Entities[i].Type == "Emotion")
                {
                    askingEmotion = true;
                    //Pour le moment, on gère HAPPY - NEUTRAL - SAD (à modifier une fois Dico OK)
                    string emotion = result.Entities[i].Entity;
                    if (emotion == "heureux" || emotion == "heureuse" || emotion == "heureuses")
                    {
                        emotion = "Happiness";
                        emotionReturn = " heureux(ses)";
                    }
                    if (emotion == "neutre" || emotion == "neutres")
                    {
                        emotion = "Neutral";
                        emotionReturn = " neutres";
                    }
                    if (emotion == "triste" || emotion == "tristes")
                    {
                        emotion = "Sadness";
                        emotionReturn = " tristes";
                    }
                    for (int y = 0; y < nbVisits; y++)
                    {
                        int nbEmotion = tmp[y].ProfilePicture.Count();
                        for (int z = 0; z < nbEmotion; z++)
                        {
                            if (customDialog.getEmotion(tmp[y].ProfilePicture[z].EmotionScore) == emotion &&
                            customDialog.getEmotion(tmp[y].ProfilePicture[z].EmotionScore) != null)
                            {
                                visitsReturn.Add(tmp[y]);
                                EmotionPicture.Add(tmp[y].ProfilePicture[z]);
                            }
                        }
                    }
                }
            }
            //CompositeEntities 
            if (result.CompositeEntities != null)
            {
                nbEntities = result.CompositeEntities.Count();
                for (int i = 0; i < nbEntities; i++)
                {
                    if (visitsReturn.Count() != 0)
                    {
                        nbVisits = visitsReturn.Count();
                        tmp = visitsReturn.ToList();
                        visitsReturn.Clear();
                    }

                    //Process of ages
                    if (result.CompositeEntities[i].ParentType == "SingleAge")
                    {
                        int age = Convert.ToInt32(result.CompositeEntities[i].Children[0].Value);
                        ageReturn = " de " + age + " ans";
                        for (int y = 0; y < nbVisits; y++)
                        {
                            agePerson = DateTime.Today.Year - tmp[y].PersonVisit.Age;
                            if (agePerson == age)
                            {
                                visitsReturn.Add(tmp[y]);
                            }
                        }
                    }
                    else if (result.CompositeEntities[i].ParentType == "IntervalAge")
                    {
                        int age = Convert.ToInt32(result.CompositeEntities[i].Children[0].Value);
                        int age2 = Convert.ToInt32(result.CompositeEntities[i].Children[1].Value);
                        if (age2 < age && age2 != -1)
                        {
                            int ageTmp = age;
                            age = age2;
                            age2 = ageTmp;
                        }
                        ageReturn = " entre " + age + " et " + age2 + " ans";
                        for (int y = 0; y < nbVisits; y++)
                        {
                            agePerson = DateTime.Today.Year - tmp[y].PersonVisit.Age;
                            if (agePerson >= age && agePerson <= age2)
                            {
                                visitsReturn.Add(tmp[y]);
                            }
                        }
                    }
                }
            }
            //Return results
            replyToConversation = ((Activity)context.Activity).CreateReply("J'ai vu " + visitsReturn.Count() + " " + genderReturn + " " + emotionReturn + " " + ageReturn + ".");
            replyToConversation.Recipient = context.Activity.From; 
            replyToConversation.Type = "message";
            replyToConversation.AttachmentLayout = "carousel";
            replyToConversation.Attachments = new List<Attachment>();
            int compteur = 0;
            foreach (var visit in visitsReturn)
            {
                List<CardImage> cardImages = new List<CardImage>();
                if (!askingEmotion)
                {
                    cardImages.Add(new CardImage(url: $"{ ConfigurationManager.AppSettings["WebAppUrl"] }{VirtualPathUtility.ToAbsolute(visit.ProfilePicture.Last().Uri)}")); // a mettre dans le SDK
                }
                else
                {
                    cardImages.Add(new CardImage(url: $"{ ConfigurationManager.AppSettings["WebAppUrl"] }{VirtualPathUtility.ToAbsolute(EmotionPicture[compteur].Uri)}")); // a mettre dans le SDK
                }
                //Calcul la bonne année et la bonne heure.
                DateTime today = DateTime.Today;
                int wrongDate = visit.PersonVisit.DateVisit.Year;
                int goodDate = DateTime.Today.Year - wrongDate;
                string messageDate = "";
                string firstname = "";
                DateTime visitDate = visit.PersonVisit.DateVisit;

                if (visit.PersonVisit.FirstName == null)
                {
                    firstname = "une personne inconnue";
                }
                else
                {
                    firstname = visit.PersonVisit.FirstName;
                }
                messageDate = customDialog.GetVisitsMessage(firstname, visitDate);

                HeroCard plCard = new HeroCard()
                {
                    Title = firstname,
                    Text = messageDate + " (" + Convert.ToString(visit.PersonVisit.DateVisit.AddHours(1).AddYears(goodDate).ToString("dd/MM/yyyy")) + ")",
                    //Subtitle = 
                    Images = cardImages
                    //Buttons = cardButtons
                };

                Attachment plAttachment = plCard.ToAttachment();
                replyToConversation.Attachments.Add(plAttachment);
                compteur++;
            }
            await context.PostAsync(replyToConversation);
            context.Wait(MessageReceived);
        }

        [LuisIntent("GetVisitsPersonByFirstname")]
        public async Task GetVisitsPersonByFirstname(IDialogContext context, LuisResult result)
        {
            Activity replyToConversation = null;
            AdaClient client = new AdaClient();

            int nbVisit = 10;

            if (result.Entities[0].Type == "ChoosePersonId")
            {
                var splitResult = result.Entities[0].Entity.Split(':');

                int idPerson = Convert.ToInt32(splitResult[1]);

                nbVisit = Convert.ToInt32(splitResult[3]);

                List<VisitDto> visitsById = await client.GetVisitPersonById(idPerson, nbVisit);

                string reply = "Je l'ai vu(e) à ces dates : ";
                reply += Environment.NewLine;

                foreach (var visit in visitsById)
                {
                    reply += "     -" + Convert.ToString(visit.Date.AddHours(1));
                }

                replyToConversation = ((Activity)context.Activity).CreateReply(reply);
            }
            else
            {
                string firstname = null;

                int nbEntities = result.Entities.Count();
                for (int i = 0; i < nbEntities; i++)
                {
                    if (result.Entities[i].Type == "Firstname")
                    {
                        firstname = result.Entities[i].Entity;
                    }
                    if (result.Entities[i].Type == "Number")
                    {
                        nbVisit = Convert.ToInt32(result.Entities[i].Entity);
                    }
                }

                List<VisitDto> visits = await client.GetLastVisitPerson(firstname);

                if (visits.Count == 0)
                {
                    replyToConversation = ((Activity)context.Activity).CreateReply("Je n'ai pas encore rencontré " + firstname + " :/ Il faudrait nous présenter! ^^");
                    replyToConversation.Recipient = context.Activity.From;
                    replyToConversation.Type = "message";
                }
                else if (visits.Count == 1)
                {
                    int id = visits[0].PersonVisit.PersonId;

                    List<VisitDto> visitsById = await client.GetVisitPersonById(id, nbVisit);

                    string reply = "J'ai vu " + firstname + " à ces dates : ";
                    reply += Environment.NewLine;

                    foreach (var visit in visitsById)
                    {
                        reply += "     -" + Convert.ToString(visit.Date.AddHours(1));
                    }

                    replyToConversation = ((Activity)context.Activity).CreateReply(reply);
                }
                else
                {
                    replyToConversation = ((Activity)context.Activity).CreateReply("Je connais " + visits.Count + " " + firstname + ". Les voici :)");
                    replyToConversation.Recipient = context.Activity.From;
                    replyToConversation.Type = "message";
                    replyToConversation.AttachmentLayout = "carousel";
                    replyToConversation.Attachments = new List<Attachment>();

                    foreach (var visit in visits)
                    {
                        List<CardImage> cardImages = new List<CardImage>();
                        cardImages.Add(new CardImage(url: $"{ ConfigurationManager.AppSettings["WebAppUrl"] }{VirtualPathUtility.ToAbsolute(visit.ProfilePicture.Last().Uri)}")); // a mettre dans le SDK

                        List<CardAction> cardButtons = new List<CardAction>();

                        CardAction plButtonChoice = new CardAction()
                        {

                            Value = "ChoosePersonId :" + visit.PersonVisit.PersonId + " : nbVisit :" + nbVisit,
                            Type = "postBack",
                            Title = "Le voilà !"
                        };
                        cardButtons.Add(plButtonChoice);

                        HeroCard plCard = new HeroCard()
                        {
                            Title = visit.PersonVisit.FirstName,
                            Images = cardImages,
                            Buttons = cardButtons
                        };

                        Attachment plAttachment = plCard.ToAttachment();
                        replyToConversation.Attachments.Add(plAttachment);
                    }
                }
            }

            await context.PostAsync(replyToConversation);
            context.Wait(MessageReceived);
        }
    }
}